using Arch.Core;
using Arch.System;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Systems;
using Shared.Infrastructure.ECS.Tags;
using Shared.Infrastructure.Network.Data.Join;
using Shared.Infrastructure.Network.Data.Left;
using Shared.Infrastructure.Network.Repository;
using Shared.Infrastructure.Network.Transport;

namespace Game.Server.Headless.Infrastructure.ECS.Systems.Network.Receive;

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
    private readonly List<IDisposable> _disposables = [];

    public NetworkToEntitySystem(
        World world, 
        ILogger<NetworkToEntitySystem> logger, 
        NetworkReceiver receiver, 
        NetworkSender sender, 
        PeerRepository peerRepository,
        EntitySystem entitySystem) : base(world)
    {
        _logger = logger;
        _sender = sender;
        _entitySystem = entitySystem;
        _peerRepository = peerRepository;
        
        peerRepository.PeerDisconnected += LeftPlayerByConnection;

        // Registra os manipuladores de mensagens para JoinRequest e LeftRequest
        _disposables.AddRange([
            receiver.RegisterMessageHandler<JoinRequest>(OnJoinRequestReceived),
            receiver.RegisterMessageHandler<LeftRequest>(OnLeftRequestReceived)
        ]);
    }

    private void LeftPlayerByConnection(NetPeer peer, string reason)
    {
        var request = new LeftRequest();
        OnLeftRequestReceived(request, peer);
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
            GridPosition = new(5, 5) // Posição inicial
        };

        if (!_entitySystem.CreatePlayerEntity(newPlayerData, out var newPlayerEntity))
        {
            _logger.LogError($"[PlayerSpawner] Failed to create player entity for ID: {peer.Id}");
            return; // Se falhar, não continua o processo
        }

        // 2. Notificar TODOS (incluindo o novo) sobre o novo jogador.
        var players = _entitySystem.GetPlayerEntities();
        _sender.EnqueueReliableBroadcast(ref newPlayerData);

        // 3. Notificar APENAS o novo jogador sobre os outros.
        // Itera sobre o dicionário de jogadores. É um pouco mais eficiente que .Values.Where(...)
        foreach (var (otherPlayerId, otherPlayerEntity) in players)
        {
            // Pula a iteração se for o jogador que acabou de entrar
            if (otherPlayerId == peer.Id) continue;

            // Monta o pacote com os dados atuais do jogador existente
            var playerInfo = World.Get<PlayerInfoComponent>(otherPlayerEntity);
            var existingPlayerData = new PlayerData
            {
                NetId = World.Get<NetworkedTag>(otherPlayerEntity).Id,
                GridPosition = World.Get<GridPositionComponent>(otherPlayerEntity).Value,
                Direction = World.Get<DirectionComponent>(otherPlayerEntity).Value,
                Speed = World.Get<SpeedComponent>(otherPlayerEntity).Value,
                Name = playerInfo.Name,
                Vocation = playerInfo.Vocation,
                Gender = playerInfo.Gender
            };

            // Enfileira um pacote para cada jogador existente.
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