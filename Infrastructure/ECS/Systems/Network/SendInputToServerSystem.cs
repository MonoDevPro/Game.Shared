using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Infrastructure.ECS.Components;
using Godot;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Data.Input;

namespace GameClient.Infrastructure.ECS.Systems.Network;

public partial class SendInputToServerSystem(World world, NetworkManager manager) : BaseSystem<World, float>(world)
{
    [Query]
    [All<PlayerControllerTag, MoveIntentCommand>]
    private void SendIntentToServer(in MoveIntentCommand intent)
    {
        var packet = new MovementRequest { Direction = intent.Direction };
        manager.Sender.EnqueueReliableSend(0, ref packet);
        
        GD.Print("Enviando intenção de movimento para o servidor: " + intent.Direction);
    }
}