using Arch.Core;
using Arch.System;
using Game.Server.Headless.Infrastructure.ECS.Systems.Process;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Tags;
using Shared.Infrastructure.Math;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Data.Input;
using Shared.Infrastructure.Network.Data.Join;
using Shared.Infrastructure.Network.Data.Left;
using Shared.Infrastructure.Network.Repository;
using Shared.Infrastructure.Network.Transport;

namespace Game.Server.Headless.Infrastructure.ECS.Systems.Physics;

/// <summary>
/// No servidor, converte o InputRequest (intenção de movimento) da rede 
/// em um MoveIntentCommand para ser processado pelo ProcessMovementSystem.
/// </summary>
public partial class NetworkToCommandSystem (
    ILogger<NetworkToCommandSystem> logger,
    World world, 
    NetworkManager networkManager,
    EntitySystem entitySystem) : BaseSystem<World, float>(world)
{
    private readonly List<IDisposable> _playerPacketsDisposables = [];
    
    private readonly NetworkSender _sender = networkManager.Sender;
    private readonly NetworkReceiver _receiver = networkManager.Receiver;
    private readonly PeerRepository _repository = networkManager.PeerRepository;
    
    public override void Initialize()
    {
        _repository.PeerDisconnected += OnPeerDisconnected;
        
        _playerPacketsDisposables.AddRange(
        [
            _receiver.RegisterMessageHandler<JoinRequest>(RequestPlayerJoin),
            _receiver.RegisterMessageHandler<LeftRequest>(RequestPlayerLeft),
            _receiver.RegisterMessageHandler<MovementRequest>(OnMovementRequestReceived)
        ]);
        
        base.Initialize();
    }
    
    public override void Dispose()
    {
        _repository.PeerDisconnected -= OnPeerDisconnected;
        
        // Dispose all message handlers
        foreach (var disposable in _playerPacketsDisposables)
            disposable.Dispose();
        
        base.Dispose();
    }
    
    public override void Update(in float t) => networkManager.PollEvents();
    
    private void OnPeerDisconnected(NetPeer peer, string reason)
    {
        logger.LogInformation($"[PlayerSpawner] Player Disconnected with ID: {peer.Id} reason: {reason}");
        RequestPlayerLeft(new LeftRequest(), peer);
    }
    private void RequestPlayerJoin(JoinRequest packet, NetPeer peer)
    {
        logger.LogInformation($"Player '{packet.Name}' (ID: {peer.Id}) está tentando entrar.");
        
        // 1. Criar os dados e a entidade para o NOVO jogador.
        var newPlayerData = new PlayerData
        {
            Name = packet.Name,
            NetId = peer.Id,
            Vocation = packet.Vocation,
            Gender = packet.Gender,
            GridPosition = new GridVector(5, 5)
        };
        if (!entitySystem.CreatePlayerEntity(ref newPlayerData, out var newPlayerEntity))
        {
            logger.LogError($"[PlayerSpawner] Failed to create player entity for ID: {peer.Id}");
            return; // Se falhar, não continua o processo
        }

        // 2. Notificar TODOS (incluindo o novo) sobre o novo jogador.
        var players = entitySystem.GetPlayers();
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
    
    public void RequestPlayerLeft(LeftRequest packet, NetPeer peer)
    {
        if (entitySystem.DisposePlayer(peer.Id))
        {
            var leftResponse = new LeftResponse() { NetId = peer.Id };
            _sender.EnqueueReliableBroadcast(ref leftResponse);
            logger.LogInformation($"[PlayerSpawner] Player Left with ID: {peer.Id}");
        }
    }
    
    private void OnMovementRequestReceived(MovementRequest packet, NetPeer peer)
    {
        if (!entitySystem.TryGetPlayerByNetId(peer.Id, out var entity))
            return;
        
        // IMPORTANTE: Previne que o cliente envie múltiplos movimentos antes do servidor processar o primeiro.
        // Se a entidade já tem uma intenção pendente ou já está se movendo, ignora o novo request.
        if (World.Has<MoveIntentCommand>(entity))
            return;
        
        // Adiciona o comando com a direção recebida e a tag para bloquear novos movimentos.
        World.Add(entity, new MoveIntentCommand { Direction = packet.Direction });
    }
}