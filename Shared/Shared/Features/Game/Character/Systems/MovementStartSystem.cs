using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Shared.Core.Common.Constants;
using Shared.Core.Common.Helpers;
using Shared.Features.Player.Components;
using Shared.Features.Player.Components.Commands;
using Shared.Features.Player.Components.Tags;
using Shared.Infrastructure.Math;
using Shared.Infrastructure.WorldGame;

namespace Shared.Features.Player.Systems;

public partial class MovementStartSystem(World world, GameMap gameMap, ILogger<MovementProcessSystem> logger) 
    : BaseSystem<World, float>(world)
{
    private const int GridSize = GameMapConstants.GridSize;
    
    // Parte 1: Inicia o movimento. Válido para Cliente (predição) e Servidor (autoridade).
    [Query]
    [All<NetworkedTag, MoveIntentCommand, GridPositionComponent, SpeedComponent>]
    [None<MovementProgressComponent>]
    private void StartMovement(in Entity entity, 
        ref DirectionComponent dir, ref GridPositionComponent gridPos, in SpeedComponent speed, 
        in MoveIntentCommand intent)
    {
        GridVector targetGridPos = gridPos.Value + intent.Direction;

        if (!gameMap.IsTileWalkable(targetGridPos))
        {
            World.Remove<MoveIntentCommand>(entity);
            logger.LogWarning("Movimento inválido na direção {Direction} do nó {Entity}.", intent.Direction, entity);
            return;
        }
        // Converte o vetor de movimento (ex: {X:1, Y:0}) para a enumeração (ex: DirectionEnum.East)
        dir.Value = intent.Direction.ToDirection();
            
        var startPixelPos = new WorldPosition(gridPos.Value.X * GridSize, gridPos.Value.Y * GridSize);
        var targetPixelPos = new WorldPosition(targetGridPos.X * GridSize, targetGridPos.Y * GridSize);
            
        var distance = startPixelPos.DistanceTo(targetPixelPos);
        var duration = speed.Value > 0 ? distance / speed.Value : 0f;

        // Adiciona o componente que representa o ESTADO do movimento.
        World.Add(entity, new MovementProgressComponent
        {
            StartPosition = startPixelPos,
            TargetPosition = targetPixelPos,
            Duration = duration,
            TimeElapsed = 0f
        });
    }
}