using Arch.Core;
using Arch.System;
using Game.Server.Headless.Infrastructure.ECS.Components;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;


// TODO: Este sistema está desativado por enquanto, não é necessário no estado atual do jogo.
//       Ele pode ser reativado quando o sistema de movimento for implementado e as entidades
//       começarem a se mover entre células. Por enquanto, o InterestSystem não é usado
//       e não está integrado com o MovementSystem. Ele deve ser reativado quando necessário.
namespace Game.Server.Headless.Infrastructure.ECS.Systems.Process
{
    // TODO: --- NOVA LÓGICA DE ENVIO DIRECIONADO ---
    // Itera apenas sobre os NetIds que estão na área de interesse desta entidade.
    /*foreach (var interestedPeerId in interest.EntitiesInInterest)
    {
        // Envia o pacote de forma confiável para cada peer interessado.
        sender.EnqueueReliableSend(interestedPeerId, ref packet);
    }*/
    
    
    /// <summary>
    /// Componente exclusivo do servidor que gerencia o estado da Área de Interesse (AoI) de uma entidade.
    /// Armazena quais outras entidades estão atualmente na AoI desta entidade.
    /// </summary>
    public struct InterestManagementComponent()
    {
        /// <summary>
        /// Um HashSet contendo os NetIds das entidades que estão atualmente na AoI desta entidade.
        /// Usamos HashSet para buscas, adições e remoções rápidas (O(1) em média).
        /// </summary>
        public HashSet<int> EntitiesInInterest = [];
    }
    
    /// <summary>
    /// Sistema de Interest com Spatial-Hash (uniform grid) + batching + throttling + hysteresis.
    /// - Mantém uma grid (células) que aponta para NetIds (inteiros).
    /// - Atualiza apenas entidades "dirty" (que mudaram de célula).
    /// - Agrupa sends em CreateMany / DestroyMany por cliente por tick.
    ///
    /// Observações de integração:
    /// - MovementSystem deve chamar NotifyEntityMoved(entity) sempre que a entidade trocar de célula
    ///   (ou então este sistema pode ser adaptado para observar um componente "MovedThisTick").
    /// - Adapte os tipos de pacote CreateManyPacket/DestroyManyPacket caso sua serialização de rede seja diferente.
    /// </summary>
    /*public sealed class InterestSystem : BaseSystem<World, float>
    {
        // Configuráveis
        private readonly int _cellSizeTiles; // largura/altura da célula em tiles
        private readonly float _interestRadiusTiles; // raio em tiles
        private readonly float _tickInterval; // em segundos (ex: 0.2 => 5Hz)

        // Histerese: enter < exit para evitar churn
        private readonly float _enterRadiusSq;
        private readonly float _exitRadiusSq;

        // Estruturas internas
        // cellKey -> lista de NetIds
        private readonly Dictionary<long, List<int>> _cells = new();
        // NetId -> cellKey
        private readonly Dictionary<int, long> _entityCell = new();
        // NetId -> Entity (para consulta rápida)
        private readonly Dictionary<int, Entity> _netIdToEntity = new();
        // NetId -> GridPositionComponent.Value (última conhecida) - opcional cache
        private readonly Dictionary<int, object> _positionCache = new();

        // Dirty set: entidades que se moveram de célula nesta janela
        private readonly HashSet<int> _dirty = new();

        // Pools / buffers
        private readonly List<int> _neighborBuffer = new();

        // Batching
        private readonly Dictionary<int, List<PlayerData>> _createsByOwner = new();
        private readonly Dictionary<int, List<int>> _destroysByOwner = new();

        // Dependências
        private readonly ILogger<InterestSystem> _logger;
        private readonly NetworkSender _sender;
        private readonly EntitySystem _entitySystem;

        // Contador de tempo
        private float _accum;

        // Query para entidades networked (usado apenas na inicialização)
        private readonly QueryDescription _networkedQuery = new QueryDescription()
            .WithAll<NetworkedTag, GridPositionComponent, InterestManagementComponent>();

        public InterestSystem(World world, ILogger<InterestSystem> logger, NetworkSender sender, EntitySystem entitySystem,
            int cellSizeTiles = 8, float interestRadiusTiles = 10f, float tickInterval = 0.2f)
            : base(world)
        {
            _logger = logger;
            _sender = sender;
            _entitySystem = entitySystem;

            _cellSizeTiles = Math.Max(1, cellSizeTiles);
            _interestRadiusTiles = Math.Max(1f, interestRadiusTiles);
            _tickInterval = Math.Max(0.01f, tickInterval);

            // calcular histerese (20% de tolerância por default)
            _enterRadiusSq = _interestRadiusTiles * _interestRadiusTiles;
            var exitRadius = _interestRadiusTiles * 1.2f;
            _exitRadiusSq = exitRadius * exitRadius;

            // inicializar registrando todas as entidades existentes
            BuildInitialSpatialHash();
        }

        /// <summary>
        /// Reconstrói a grid a partir do World. Chamado no construtor.
        /// </summary>
        private void BuildInitialSpatialHash()
        {
            // Limpa tudo
            _cells.Clear();
            _entityCell.Clear();
            _netIdToEntity.Clear();
            _positionCache.Clear();

            // Uma única Query para coletar todas as entidades networked
            World.Query(in _networkedQuery, (Entity e, ref NetworkedTag netTag, ref GridPositionComponent pos, ref InterestManagementComponent im) =>
            {
                var netId = netTag.Id;
                var cellKey = ComputeCellKey(pos.Value);

                if (!_cells.TryGetValue(cellKey, out var list))
                {
                    list = new List<int>();
                    _cells[cellKey] = list;
                }

                list.Add(netId);
                _entityCell[netId] = cellKey;
                _netIdToEntity[netId] = e;
                _positionCache[netId] = pos.Value;
            });

            _logger.LogInformation("InterestSystem inicializado. Células: {CellCount}, Entidades: {EntityCount}", _cells.Count, _netIdToEntity.Count);
        }

        /// <summary>
        /// Deve ser chamado pelo MovementSystem quando uma entidade efetivamente trocar de célula.
        /// (padrão: chamar sempre que a entidade se mover em grid).
        /// </summary>
        public void NotifyEntityMoved(Entity entity)
        {
            // tenta obter netId
            if (World.Has<NetworkedTag>(entity) == false) return;

            var netId = World.Get<NetworkedTag>(entity).Id;
            var pos = World.Get<GridPositionComponent>(entity).Value;
            var newCell = ComputeCellKey(pos);

            if (_entityCell.TryGetValue(netId, out var oldCell))
            {
                if (oldCell == newCell)
                {
                    // não trocou de célula, mas atualiza posição cache
                    _positionCache[netId] = pos;
                    return;
                }

                // remove do oldCell
                if (_cells.TryGetValue(oldCell, out var oldList))
                {
                    oldList.Remove(netId);
                }
            }

            // adiciona ao newCell
            if (!_cells.TryGetValue(newCell, out var newList))
            {
                newList = new List<int>();
                _cells[newCell] = newList;
            }

            newList.Add(netId);
            _entityCell[netId] = newCell;
            _positionCache[netId] = pos;
            _netIdToEntity[netId] = entity;

            // marca dirty: tanto a entidade que se moveu quanto as entidades nas células adjacentes
            _dirty.Add(netId);

            foreach (var neighborNetId in GetNetIdsInAdjacentCells(newCell))
            {
                _dirty.Add(neighborNetId);
            }
        }

        /// <summary>
        /// Remove a entidade do spatial-hash (ex: quando ela é destruída / desconecta).
        /// </summary>
        public void UnregisterEntity(Entity entity)
        {
            if (World.Has<NetworkedTag>(entity) == false) return;
            var netId = World.Get<NetworkedTag>(entity).Id;

            if (_entityCell.TryGetValue(netId, out var cell))
            {
                if (_cells.TryGetValue(cell, out var list)) list.Remove(netId);
                _entityCell.Remove(netId);
            }

            _positionCache.Remove(netId);
            _netIdToEntity.Remove(netId);
            _dirty.Remove(netId);
        }

        public override void Update(in float dt)
        {
            _accum += dt;
            if (_accum < _tickInterval) return;
            _accum -= _tickInterval;

            ProcessDirtyEntities();
            // limpar buffers de batching
            _createsByOwner.Clear();
            _destroysByOwner.Clear();
        }

        private void ProcessDirtyEntities()
        {
            if (_dirty.Count == 0) return;

            // Para cada dirty entity, procure vizinhos candidatos nas células adjacentes
            // e compare distâncias com enter/exit thresholds.

            foreach (var netId in _dirty)
            {
                if (!_netIdToEntity.TryGetValue(netId, out var entity)) continue;
                if (!World.Has<InterestManagementComponent>(entity)) continue; // deveria existir

                var interest = World.Get<InterestManagementComponent>(entity);
                var posA = World.Get<GridPositionComponent>(entity).Value;

                _neighborBuffer.Clear();

                // busca candidatos em células adjacentes
                var cellKey = _entityCell.TryGetValue(netId, out var ck) ? ck : ComputeCellKey(posA);
                foreach (var candidateNet in GetNetIdsInAdjacentCells(cellKey))
                {
                    if (candidateNet == netId) continue;
                    _neighborBuffer.Add(candidateNet);
                }

                // checa cada candidato
                foreach (var otherNetId in _neighborBuffer)
                {
                    if (!_netIdToEntity.TryGetValue(otherNetId, out var otherEntity)) continue;
                    var posB = World.Get<GridPositionComponent>(otherEntity).Value;

                    // Calcula distância ao quadrado usando o tipo existente na Value
                    // Assumimos que pos.Value possui um método DistanceSquaredTo (mesmo usado no código original).
                    float distSq = posA.DistanceSquaredTo(posB);

                    var isInsideEnter = distSq <= _enterRadiusSq;
                    var isOutsideExit = distSq > _exitRadiusSq;

                    // Se está dentro do enter e ainda não está no conjunto -> criar
                    if (isInsideEnter)
                    {
                        if (interest.EntitiesInInterest.Add(otherNetId))
                        {
                            // marcou como entrou
                            EnqueueCreatePair(ownerNetId: netId, entityToCreateNetId: otherNetId);
                            EnqueueCreatePair(ownerNetId: otherNetId, entityToCreateNetId: netId);
                            _logger.LogDebug("Peer {NearbyId} entrou na AoI de {OwnerId}", otherNetId, netId);
                        }
                    }
                    else if (isOutsideExit)
                    {
                        if (interest.EntitiesInInterest.Remove(otherNetId))
                        {
                            EnqueueDestroyPair(ownerNetId: netId, entityToDestroyNetId: otherNetId);
                            EnqueueDestroyPair(ownerNetId: otherNetId, entityToDestroyNetId: netId);
                            _logger.LogDebug("Peer {OldId} saiu na AoI de {OwnerId}", otherNetId, netId);
                        }
                    }
                    // se fica entre enter e exit -> mantém estado atual (histerese)
                }

                // nota: não removemos aqui entidades que não aparecem nos vizinhos (por exemplo entidades que morreram).
            }

            // Após processar todos, faça batches de envio por owner
            FlushNetworkBatches();

            // limpar dirty set (poderíamos manter alguns, mas simplificamos)
            _dirty.Clear();
        }

        private void EnqueueCreatePair(int ownerNetId, int entityToCreateNetId)
        {
            if (!_createsByOwner.TryGetValue(ownerNetId, out var list))
            {
                list = new List<PlayerData>();
                _createsByOwner[ownerNetId] = list;
            }

            var entity = _netIdToEntity.TryGetValue(entityToCreateNetId, out var e) ? e : Entity.Null;
            if (entity == Entity.Null) return;

            var playerInfo = World.Get<PlayerInfoComponent>(entity);
            var playerData = new PlayerData
            {
                NetId = entityToCreateNetId,
                Name = playerInfo.Name,
                Vocation = playerInfo.Vocation,
                Gender = playerInfo.Gender,
                Direction = World.Get<DirectionComponent>(entity).Value,
                Speed = World.Get<SpeedComponent>(entity).Value,
                GridPosition = World.Get<GridPositionComponent>(entity).Value
            };

            list.Add(playerData);
        }

        private void EnqueueDestroyPair(int ownerNetId, int entityToDestroyNetId)
        {
            if (!_destroysByOwner.TryGetValue(ownerNetId, out var list))
            {
                list = new List<int>();
                _destroysByOwner[ownerNetId] = list;
            }

            list.Add(entityToDestroyNetId);
        }

        /// <summary>
        /// Envia um pacote por owner com todas as criações e destruições acumuladas.
        /// </summary>
        private void FlushNetworkBatches()
        {
            // Envia criações batched
            foreach (var kv in _createsByOwner)
            {
                var owner = kv.Key;
                var players = kv.Value;

                var packet = new CreateManyPacket { Players = players.ToArray() };
                _sender.EnqueueReliableSend(owner, ref packet);
            }

            // Envia destruições batched
            foreach (var kv in _destroysByOwner)
            {
                var owner = kv.Key;
                var ids = kv.Value;

                var packet = new DestroyManyPacket { NetIds = ids.ToArray() };
                _sender.EnqueueReliableSend(owner, ref packet);
            }
        }

        #region Spatial helpers

        private long ComputeCellKey(dynamic gridPosition)
        {
            // gridPosition.Value deve conter coordenadas X, Y em tiles (float ou int).
            // Normalizamos para inteiros de célula.
            int cellX = (int)Math.Floor((double)(gridPosition.X) / _cellSizeTiles);
            int cellY = (int)Math.Floor((double)(gridPosition.Y) / _cellSizeTiles);
            return PackCell(cellX, cellY);
        }

        private static long PackCell(int x, int y) => ((long)x << 32) ^ (uint)y;

        /// <summary>
        /// Retorna todos NetIds nas células adjacentes (inclui a mesma célula).
        /// </summary>
        private IEnumerable<int> GetNetIdsInAdjacentCells(long cellKey)
        {
            // desempacota
            int cellX = (int)(cellKey >> 32);
            int cellY = (int)(cellKey & 0xFFFFFFFF);

            int radiusCells = (int)Math.Ceiling(_interestRadiusTiles / _cellSizeTiles);

            for (int dy = -radiusCells; dy <= radiusCells; dy++)
            {
                for (int dx = -radiusCells; dx <= radiusCells; dx++)
                {
                    var key = PackCell(cellX + dx, cellY + dy);
                    if (_cells.TryGetValue(key, out var list))
                    {
                        foreach (var netId in list)
                            yield return netId;
                    }
                }
            }
        }

        #endregion

        #region Network packet placeholders
        // NOTE: adapte estes structs para os tipos reais de sua camada de rede / serialização.
        // Eles devem ser structs (não classes) se EnqueueReliableSend usa "ref".

        public struct CreateManyPacket : INetSerializable
        {
            public PlayerData[] Players;
               
            public void Serialize(NetDataWriter writer)
            {
                writer.PutArray<PlayerData>(Players);
            }
           
            public void Deserialize(NetDataReader reader)
            {
                Players = reader.GetArray<PlayerData>();
            }
        }
        
        public struct DestroyManyPacket : INetSerializable
        {
            public int[] NetIds;
    
            public void Serialize(NetDataWriter writer)
            {
                writer.PutArray(NetIds);
            }
            public void Deserialize(NetDataReader reader)
            {
                NetIds = reader.GetIntArray();
            }
        }

        #endregion
    }*/
}