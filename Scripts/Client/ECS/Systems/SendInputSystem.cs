using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Scripts.Shared.ECS.Components;
using Game.Shared.Scripts.Shared.Network.Data.Input;
using Game.Shared.Scripts.Shared.Spawners;
using LiteNetLib;

namespace Game.Shared.Scripts.Client.ECS.Systems;

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