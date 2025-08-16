using Arch.Core;
using Arch.System;
using Game.Core.ECS.Components;
using Game.Core.ECS.Components.Tags;
using Game.Core.Entities.Common.ValueObjetcs;
using Game.Server.Headless.Core.ECS.Components;
using Game.Server.Headless.Infrastructure.Repositories;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Core.Network.Repository;
using Shared.Core.Network.Transport;
using Shared.Features.Game.Character.Packets.Enter;
using Shared.Features.Game.Character.Packets.Left;
using Shared.Game.Player;

namespace Game.Server.Headless.Core.ECS.Systems.Receive;

/// <summary>
/// Ouve os pacotes de rede relacionados com o ciclo de vida (Join/Left) e
/// traduz-os em chamadas para o EntitySystem.
/// </summary>
public class NetworkToEntitySystem : BaseSystem<World, float>
{
    private readonly ILogger<NetworkToEntitySystem> _logger;
    private readonly NetworkSender _sender;
    private readonly EntitySystem _entitySystem;
    private readonly PeerRepository _peerRepository;
    private readonly SessionService _sessions;
    private readonly List<IDisposable> _disposables = [];

    public NetworkToEntitySystem(
        World world,
        ILogger<NetworkToEntitySystem> logger,
        NetworkReceiver receiver,
        NetworkSender sender,
        PeerRepository peerRepository,
    EntitySystem entitySystem,
    SessionService sessions) : base(world)
    {
        _logger = logger;
        _sender = sender;
        _entitySystem = entitySystem;
        _peerRepository = peerRepository;
        _sessions = sessions;

        peerRepository.PeerDisconnected += LeftPlayerByConnection;

        // Registra os manipuladores de mensagens para JoinRequest e LeftRequest
        _disposables.AddRange([
            receiver.RegisterMessageHandler<JoinRequest>(OnJoinRequestReceived),
            receiver.RegisterMessageHandler<LeftRequest>(OnLeftRequestReceived),
            receiver.RegisterMessageHandler<EnterGameRequest>(OnEnterGameRequestReceived)
        ]);
    }

    private void LeftPlayerByConnection(NetPeer peer, string reason)
    {
        var request = new LeftRequest();
        OnLeftRequestReceived(request, peer);
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

        if (!_entitySystem.CreatePlayerEntity(newPlayerData, out var newPlayerEntity))
        {
            _logger.LogError("[PlayerSpawner] Falha ao criar entidade para ID: {PeerId}", peer.Id);
            return;
        }

        // Adiciona o componente de estado de input ao jogador recém-criado no servidor
        World.Add(newPlayerEntity, new ClientInputStateComponent { LastProcessedSequenceId = 0 });

        // Notifica TODOS (incluindo o novo) sobre o novo jogador.
        _sender.EnqueueReliableBroadcast(ref newPlayerData);

        // Notifica APENAS o novo jogador sobre os outros que já estão no jogo.
        var allPlayers = _entitySystem.GetPlayerEntities();
        foreach (var (existingPlayerId, existingPlayerEntity) in allPlayers)
        {
            if (existingPlayerId == peer.Id) continue;

            var playerInfo = World.Get<CharInfoComponent>(existingPlayerEntity);
            var gridPos = World.Get<MapPositionComponent>(existingPlayerEntity);
            var direction = World.Get<DirectionComponent>(existingPlayerEntity);
            var speed = World.Get<SpeedComponent>(existingPlayerEntity);
            var netTag = World.Get<NetworkedTag>(existingPlayerEntity);

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

    private void OnJoinRequestReceived(JoinRequest packet, NetPeer peer)
    {
        _logger.LogInformation("Jogador '{Name}' (ID: {PeerId}) a tentar entrar.", packet.Name, peer.Id);

        var newPlayerData = new PlayerData
        {
            Name = packet.Name,
            NetId = peer.Id,
            Vocation = packet.Vocation,
            Gender = packet.Gender,
            GridPosition = new MapPosition(5, 5) // Posição inicial
        };

        if (!_entitySystem.CreatePlayerEntity(newPlayerData, out var newPlayerEntity))
        {
            _logger.LogError($"[PlayerSpawner] Failed to create player entity for ID: {peer.Id}");
            return; // Se falhar, não continua o processo
        }

        // Adiciona o componente de estado de input ao jogador recém-criado no servidor
        World.Add(newPlayerEntity, new ClientInputStateComponent { LastProcessedSequenceId = 0 });

        // 2. Notificar TODOS (incluindo o novo) sobre o novo jogador.
        _sender.EnqueueReliableBroadcast(ref newPlayerData);

        // 3. Notificar APENAS o novo jogador sobre os outros que já estão no jogo.
        var allPlayers = _entitySystem.GetPlayerEntities();
        foreach (var (existingPlayerId, existingPlayerEntity) in allPlayers)
        {
            // Pula a iteração se for o jogador que acabou de entrar, pois ele já recebeu seus próprios dados no broadcast.
            if (existingPlayerId == peer.Id) continue;

            // --- LÓGICA REATORADA ---
            // Aqui, montamos um DTO `PlayerData` lendo os componentes da entidade existente.
            // Os componentes no ECS são a fonte da verdade.
            var playerInfo = World.Get<CharInfoComponent>(existingPlayerEntity);
            var gridPos = World.Get<MapPositionComponent>(existingPlayerEntity);
            var direction = World.Get<DirectionComponent>(existingPlayerEntity);
            var speed = World.Get<SpeedComponent>(existingPlayerEntity);
            var netTag = World.Get<NetworkedTag>(existingPlayerEntity);

            var existingPlayerData = new PlayerData
            {
                NetId = netTag.Id,
                Name = playerInfo.Name,
                Vocation = playerInfo.Vocation,
                Gender = playerInfo.Gender,
                Direction = direction.Value,
                Speed = speed.Value,
                GridPosition = gridPos.Value,
                Description = "Player already in game" // Descrição genérica ou vinda de um componente
            };

            // Envia um pacote de unicast confiável para o novo jogador.
            _sender.EnqueueReliableSend(peer.Id, ref existingPlayerData);
        }
    }

    private void OnLeftRequestReceived(LeftRequest packet, NetPeer peer)
    {
        _entitySystem.DisposePlayerEntity(peer.Id);
        var leftResponse = new LeftResponse { NetId = peer.Id };
        _sender.EnqueueReliableBroadcast(ref leftResponse);
        _logger.LogInformation("Jogador com ID: {PeerId} saiu.", peer.Id);
    }

    public override void Dispose()
    {
        // Limpa os manipuladores de mensagens registrados
        foreach (var disposable in _disposables)
            disposable.Dispose();
        _disposables.Clear();

        _peerRepository.PeerDisconnected -= LeftPlayerByConnection;

        _logger.LogInformation("[NetworkToEntitySystem] Desativado e manipuladores removidos.");
    }
}