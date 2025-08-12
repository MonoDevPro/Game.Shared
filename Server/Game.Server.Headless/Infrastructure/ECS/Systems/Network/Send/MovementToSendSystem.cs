using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Shared.Core.Network.Data.Input;
using Shared.Core.Network.Transport;
using Shared.Features.Player.Components;
using Shared.Features.Player.Components.Commands;
using Shared.Features.Player.Components.Tags;

namespace Game.Server.Headless.Infrastructure.ECS.Systems.Network.Send;

public partial class MovementToSendSystem(World world, NetworkSender sender, ILogger<MovementToSendSystem> logger) 
    : BaseSystem<World, float>(world)
{
    [Query]
    [All<NetworkedTag, MoveIntentCommand, MovementProgressComponent>]
    private void SendMovementUpdate(in Entity entity, in NetworkedTag netTag, 
        MoveIntentCommand command, in GridPositionComponent gridPos)
    {
        logger.LogDebug("Enviando atualização de movimento para todos exceto: {NetId}, Direção: {Direction}, Posição: {Position}",
            netTag.Id, command.Direction, gridPos.Value);
        
        // Se for o servidor, envia a atualização de movimento para os clientes.
        // Envia o pacote de movimento para todos os clientes conectados.
        var packet = new MovementStartResponse
        {
            NetId = netTag.Id,
            TargetDirection = command.Direction,
            CurrentPosition = gridPos.Value
        };
        
        sender.EnqueueReliableBroadcastExcept(netTag.Id, ref packet);
        
        // Removemos o comando de intenção de movimento, pois já foi processado.
        World.Remove<MoveIntentCommand>(entity);
    }
}