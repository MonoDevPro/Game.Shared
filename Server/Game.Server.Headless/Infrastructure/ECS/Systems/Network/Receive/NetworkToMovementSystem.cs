using Arch.Core;
using Arch.System;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Systems;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Data.Input;

namespace Game.Server.Headless.Infrastructure.ECS.Systems.Network.Receive;

/// <summary>
/// Ouve os pacotes de rede de input de movimento e traduz-os
/// em componentes MoveIntentCommand para a entidade apropriada.
/// </summary>
public class NetworkToMovementSystem : BaseSystem<World, float>
{
    private readonly EntitySystem _entitySystem;
    private readonly ILogger<NetworkToMovementSystem> _logger;

    public NetworkToMovementSystem(World world, NetworkManager networkManager, EntitySystem entitySystem, ILogger<NetworkToMovementSystem> logger) : base(world)
    {
        _entitySystem = entitySystem;
        _logger = logger;
        networkManager.Receiver.RegisterMessageHandler<MovementRequest>(OnMovementRequestReceived);
    }

    private void OnMovementRequestReceived(MovementRequest packet, NetPeer peer)
    {
        _logger.LogDebug("Recebido movimento do cliente {ClientId}: {Direction}", peer.Id, packet.Direction);
        
        if (!_entitySystem.PlayerExists(peer.Id))
            return;
        
        var entityId = _entitySystem.GetPlayerEntity(peer.Id);
        
        // Evita que o cliente envie múltiplos movimentos antes do servidor processar o primeiro.
        if (World.Has<MoveIntentCommand>(entityId))
            return;
        
        // Adiciona o comando de intenção à entidade para ser processado pelo MovementValidationSystem.
        World.Add(entityId, new MoveIntentCommand { Direction = packet.Direction });
    }
}