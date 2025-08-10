using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Tags;
using Shared.Infrastructure.Network.Data.Input;
using Shared.Infrastructure.Network.Transport;

namespace Game.Server.Headless.Infrastructure.ECS.Systems.Network.Send;

public partial class MovementToSendSystem(World world, NetworkSender sender, ILogger<MovementToSendSystem> logger) 
    : BaseSystem<World, float>(world)
{
    [Query]
    [All<NetworkedTag, MoveIntentCommand, MovementStateComponent>]
    private void SendMovementUpdate(in Entity entity, in NetworkedTag netTag, 
        MoveIntentCommand command, in GridPositionComponent gridPos)
    {
        // Se for o servidor, envia a atualização de movimento para os clientes.
        // Envia o pacote de movimento para todos os clientes conectados.
        var packet = new MovementUpdateResponse
        {
            NetId = netTag.Id,
            DirectionInput = command.Direction,
            LastGridPosition = gridPos.Value
        };
        
        sender.EnqueueReliableBroadcastExcept(netTag.Id, ref packet);
        
        // Removemos o comando de intenção de movimento, pois já foi processado.
        World.Remove<MoveIntentCommand>(entity);
    }
}