using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Infrastructure.ECS.Components;
using Godot;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.Network.Data.Input;
using Shared.Infrastructure.Network.Transport;

namespace GameClient.Infrastructure.ECS.Systems.Network.Send;

public partial class MovementToSendSystem(World world, NetworkSender sender, ILogger<MovementToSendSystem> logger) 
    : BaseSystem<World, float>(world)
{
    [Query]
    [All<PlayerControllerTag, MoveIntentCommand>]
    [None<MovementStateComponent>]
    private void SendMovementUpdate(in Entity entity, in MoveIntentCommand intent)
    {
        var inputDirection = intent.Direction;
        
        logger.LogDebug("Enviando movimento para o servidor: {Direction} do nó {Entity}.", inputDirection, entity);
        
        // Se for o servidor, envia a atualização de movimento para os clientes.
        // Envia o pacote de movimento para todos os clientes conectados.
        var packet = new MovementRequest { Direction = inputDirection, };
                
        sender.EnqueueReliableSend(0, ref packet);
        
        // Removemos o comando de intenção de movimento, pois já foi processado.
        World.Remove<MoveIntentCommand>(entity);
    }
}