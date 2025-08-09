using Arch.Core;
using Arch.System;
using Game.Server.Headless.Infrastructure.ECS.Systems.Process;
using LiteNetLib;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Tags;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Data.Input;

namespace Game.Server.Headless.Infrastructure.ECS.Systems.Network;

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
        if (!_entitySystem.TryGetPlayerByPeer(peer, out var entity))
            return;
        
        // Evita que o cliente envie múltiplos movimentos antes do servidor processar o primeiro.
        if (World.Has<MoveIntentCommand>(entity) || World.Has<IsMovingTag>(entity))
            return;
        
        // Adiciona o comando de intenção à entidade para ser processado pelo MovementValidationSystem.
        World.Add(entity, new MoveIntentCommand { Direction = packet.Direction });
    }
}