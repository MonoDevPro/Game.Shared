using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Game.Shared.Shared.Infrastructure.Network.Data.Input;
using Game.Shared.Shared.Infrastructure.Spawners;
using LiteNetLib;

namespace Game.Shared.Client.Infrastructure.ECS.Systems;

public partial class SendInputSystem(World world, PlayerSpawner spawner) : BaseSystem<World, float>(world)
{
    [Query]
    [All<PlayerControllerTag, MoveIntentCommand>]
    private void SendIntentToServer(in Entity entity, in MoveIntentCommand intent)
    {
        var packet = new InputRequest { Direction = intent.Direction };
        spawner.NetworkManager.Sender.SendToServer(ref packet, DeliveryMethod.ReliableOrdered);
        
        // Remove o comando após enviá-lo.
        World.Remove<MoveIntentCommand>(entity);
    }
}