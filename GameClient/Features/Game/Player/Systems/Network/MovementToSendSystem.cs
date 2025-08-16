using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Core.ECS.Components;
using Game.Core.ECS.Components.Commands;
using GameClient.Features.Game.Player.Components;
using Microsoft.Extensions.Logging;
using Shared.Core.Network.Transport;
using Shared.Game.Player;

namespace GameClient.Features.Game.Player.Systems.Network;

public partial class MovementToSendSystem(World world, NetworkSender sender, ILogger<MovementToSendSystem> logger) 
    : BaseSystem<World, float>(world)
{
    [Query]
    [All<PlayerControllerTag, MoveIntentCommand, MovementProgressComponent>]
    private void SendMovementUpdate(in Entity entity, ref InputSequenceComponent seq, in MoveIntentCommand intent)
    {
        var inputDirection = intent.Direction;
        
        logger.LogDebug("Enviando movimento para o servidor: {Direction} do nó {Entity}.", inputDirection, entity);
        
        // Se for o servidor, envia a atualização de movimento para os clientes.
        // Envia o pacote de movimento para todos os clientes conectados.
        var packet = new MovementRequest { SequenceId = seq.NextId, Direction = inputDirection, };
        
        sender.EnqueueReliableSend(0, ref packet);
        
        // Incrementa o ID para o próximo pacote
        seq.NextId++;
        
        // Removemos o comando de intenção de movimento, pois já foi processado.
        World.Remove<MoveIntentCommand>(entity);
    }
}