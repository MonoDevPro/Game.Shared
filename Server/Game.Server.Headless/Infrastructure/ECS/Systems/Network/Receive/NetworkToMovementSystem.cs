using Arch.Core;
using Arch.System;
using LiteNetLib;
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

    public NetworkToMovementSystem(World world, NetworkManager networkManager, EntitySystem entitySystem) : base(world)
    {
        _entitySystem = entitySystem;
        networkManager.Receiver.RegisterMessageHandler<MovementRequest>(OnMovementRequestReceived);
    }

    private void OnMovementRequestReceived(MovementRequest packet, NetPeer peer)
    {
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