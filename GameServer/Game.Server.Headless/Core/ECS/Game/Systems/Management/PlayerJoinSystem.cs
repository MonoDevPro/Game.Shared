using Arch.Core;
using Arch.System;
using Game.Core.Common.ValueObjetcs;
using Game.Server.Headless.Core.ECS.Game.Components;
using Game.Server.Headless.Core.ECS.Game.Components.Tags;
using Game.Server.Headless.Infrastructure.Repositories;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Network.Packets.Game.Player;
using Shared.Network.Transport;

namespace Game.Server.Headless.Core.ECS.Game.Systems.Management;

/// <summary>
/// Ouve os pacotes de rede relacionados com o ciclo de vida (Join/Left) e
/// traduz-os em chamadas para o EntitySystem.
/// </summary>
public class PlayerJoinSystem : BaseSystem<World, float>
{
    private readonly ILogger<PlayerJoinSystem> _logger;
    private readonly NetworkSender _sender;
    private readonly SessionService _sessions;
    private readonly List<IDisposable> _disposables = [];
    
    public PlayerJoinSystem(
        World world,
        ILogger<PlayerJoinSystem> logger,
        NetworkReceiver receiver,
        NetworkSender sender,
        SessionService sessions) : base(world)
    {
        _logger = logger;
        _sender = sender;
        _sessions = sessions;

        // Registra os manipuladores de mensagens para JoinRequest e LeftRequest
        _disposables.AddRange([
            receiver.RegisterMessageHandler<EnterGameRequest>(OnEnterGameRequestReceived)
        ]);
    }
    
    private void OnEnterGameRequestReceived(EnterGameRequest packet, NetPeer peer)
    {
        // Valida a sessão e a posse do personagem
        if (!_sessions.TryGetAccount(peer, out var accountId))
        {
            _logger.LogWarning("EnterGame recusado: peer {PeerId} não autenticado.", peer.Id);
            return;
        }
        if (!_sessions.TryGetSelectedCharacter(peer.Id, out var ch) || ch.CharacterId != packet.CharacterId)
        {
            _logger.LogWarning("EnterGame recusado: personagem inválido {CharacterId} para conta {AccountId} (peer {PeerId}).", packet.CharacterId, accountId, peer.Id);
            return;
        }
        var newPlayerData = new PlayerData
        {
            Name = ch.Name,
            NetId = peer.Id,
            Vocation = ch.Vocation,
            Gender = ch.Gender,
            GridPosition = new MapPosition(5, 5) // Posição inicial
        };
        
        // Apenas cria a entidade no mundo ECS. NENHUM NÓ GODOT É CRIADO.
        var newPlayerEntity = World.Create(
            new NetworkedTag { Id = newPlayerData.NetId },
            new CharInfoComponent { Name = newPlayerData.Name, Vocation = newPlayerData.Vocation, Gender = newPlayerData.Gender},
            new MapPositionComponent { Value = newPlayerData.GridPosition },
            new SpeedComponent { Value = newPlayerData.Speed },
            new DirectionComponent { Value = newPlayerData.Direction },
            new ClientInputStateComponent { LastProcessedSequenceId = 0 }
        );

        Dictionary<int, Entity> playersByNetId = new();
        
        // 2. Obtém o registro e adiciona o novo jogador.
        var registryQuery = new QueryDescription().WithAll<PlayerRegistryComponent>();
        var netId = newPlayerData.NetId;
        
        World.Query(in registryQuery, (ref PlayerRegistryComponent registry) =>
        {
            registry.PlayersByNetId[netId] = newPlayerEntity;
            playersByNetId = registry.PlayersByNetId;
        });

        // Notifica TODOS (incluindo o novo) sobre o novo jogador.
        _sender.EnqueueReliableBroadcast(ref newPlayerData);

        // Notifica APENAS o novo jogador sobre os outros que já estão no jogo.
        foreach (var existingPlayerId in playersByNetId)
        {
            if (existingPlayerId.Key == peer.Id) continue;

            var playerInfo = World.Get<CharInfoComponent>(existingPlayerId.Value);
            var gridPos = World.Get<MapPositionComponent>(existingPlayerId.Value);
            var direction = World.Get<DirectionComponent>(existingPlayerId.Value);
            var speed = World.Get<SpeedComponent>(existingPlayerId.Value);
            var netTag = World.Get<NetworkedTag>(existingPlayerId.Value);

            var existingPlayerData = new PlayerData
            {
                NetId = netTag.Id,
                Name = playerInfo.Name,
                Vocation = playerInfo.Vocation,
                Gender = playerInfo.Gender,
                Direction = direction.Value,
                Speed = speed.Value,
                GridPosition = gridPos.Value,
                Description = "Player already in game"
            };

            _sender.EnqueueReliableSend(peer.Id, ref existingPlayerData);
        }
    }

    public override void Dispose()
    {
        // Limpa os manipuladores de mensagens registrados
        foreach (var disposable in _disposables)
            disposable.Dispose();
        _disposables.Clear();

        _logger.LogInformation("[NetworkToEntitySystem] Desativado e manipuladores removidos.");
    }
}