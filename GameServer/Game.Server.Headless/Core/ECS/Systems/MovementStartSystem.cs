using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Core.ECS.Components;
using Game.Core.ECS.Components.Commands;
using Game.Core.ECS.Components.Tags;
using Game.Core.Entities.Common.Constants;
using Game.Core.Entities.Common.Helpers;
using Game.Core.Entities.Common.ValueObjetcs;
using Game.Core.Entities.Map;
using Microsoft.Extensions.Logging;
using Shared.Core.Network.Transport;
using Shared.Game.Player;

namespace Game.Server.Headless.Core.ECS.Systems;

public partial class MovementStartSystem(World world, GameMap gameMap, NetworkSender sender, ILogger<MovementProcessSystem> logger) 
    : BaseSystem<World, float>(world)
{
    private const int GridSize = GameMapConstants.GridSize;
    
    // Parte 1: Inicia o movimento. Válido para Cliente (predição) e Servidor (autoridade).
    [Query]
    [All<NetworkedTag, MoveIntentCommand, MapPositionComponent, SpeedComponent>]
    [None<MovementProgressComponent>]
    private void StartMovement(in Entity entity, 
        ref DirectionComponent dir, ref MapPositionComponent mapPos, in SpeedComponent speed, 
        in MoveIntentCommand intent)
    {
        MapPosition targetGridPos = mapPos.Value + intent.Direction;

        if (!gameMap.IsTileWalkable(targetGridPos))
        {
            World.Remove<MoveIntentCommand>(entity);
            logger.LogWarning("Movimento inválido na direção {dir} do nó {Entity}.", intent.Direction, entity);
            return;
        }
        // Converte o vetor de movimento (ex: {X:1, Y:0}) para a enumeração (ex: DirectionEnum.East)
        dir.Value = intent.Direction.ToDirection();

        var distance = MovementHelper.CalculateMovementDuration(mapPos.Value, targetGridPos, speed.Value);
        var duration = speed.Value > 0 ? distance / speed.Value : 0f;

        // Adiciona o componente que representa o ESTADO do movimento.
        World.Add(entity, new MovementProgressComponent
        {
            StartPosition = mapPos.Value,
            TargetPosition = targetGridPos,
            Duration = duration,
            TimeElapsed = 0f
        });
    }
    
    [Query]
    [All<NetworkedTag, MoveIntentCommand, MovementProgressComponent>]
    private void SendMovementUpdate(in Entity entity, in NetworkedTag netTag, 
        MoveIntentCommand command, in MapPositionComponent gridPos)
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