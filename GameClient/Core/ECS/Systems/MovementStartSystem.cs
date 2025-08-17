using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Core.Common.Constants;
using Game.Core.Common.Helpers;
using Game.Core.Common.ValueObjetcs;
using Game.Core.Entities.Map;
using GameClient.Core.ECS.Components;
using GameClient.Core.ECS.Components.Commands;
using GameClient.Core.ECS.Components.States;
using GameClient.Core.ECS.Components.Tags;
using Microsoft.Extensions.Logging;

namespace GameClient.Core.ECS.Systems;

public partial class MovementStartSystem(World world, GameMap gameMap, ILogger<GameClient.Core.ECS.Systems.MovementProcessSystem> logger)
    : BaseSystem<World, float>(world)
{
    private const int GridSize = GameMapConstants.GridSize;

    // Parte 1: Inicia o movimento. Válido para Cliente (predição) e Servidor (autoridade).
    [Query]
    [All<NetworkedTag, MoveIntentCommand, MapPositionComponent, SpeedComponent>]
    [None<MovementProgressComponent>]
    private void StartMovement(in Entity entity,
        ref DirectionComponent dir, ref MapPositionComponent gridPos, in SpeedComponent speed,
        in MoveIntentCommand intent)
    {
        MapPosition targetGridPos = gridPos.Value + intent.Direction;

        if (!gameMap.IsTileWalkable(targetGridPos))
        {
            World.Remove<MoveIntentCommand>(entity);
            logger.LogWarning("Movimento inválido na direção {Direction} do nó {Entity}.", intent.Direction, entity);
            return;
        }
        // Converte o vetor de movimento (ex: {X:1, Y:0}) para a enumeração (ex: DirectionEnum.East)
        dir.Value = intent.Direction.ToDirection();

        // Calcula duração baseada na distância em pixels e velocidade
        var startPixelPos = gridPos.Value.ToWorldPosition();
        var targetPixelPos = targetGridPos.ToWorldPosition();
        var pixelDistance = startPixelPos.DistanceTo(targetPixelPos);
        var duration = speed.Value > 0 ? pixelDistance / speed.Value : 0f;

        // Adiciona o componente que representa o ESTADO do movimento.
        World.Add(entity, new MovementProgressComponent
        {
            StartPosition = gridPos.Value,
            TargetPosition = targetGridPos,
            Duration = duration,
            TimeElapsed = 0f
        });
    }
}