using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Shared.Core.Constants;
using Shared.Core.Extensions;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Tags;
using Shared.Infrastructure.Math;
using Shared.Infrastructure.WorldGame;

namespace Shared.Infrastructure.ECS.Systems;

public partial class MovementStartSystem(World world, GameMap gameMap, ILogger<MovementProcessSystem> logger) 
    : BaseSystem<World, float>(world)
{
    private const int GridSize = GameMapConstants.GridSize;
    
    [Query]
    [All<NetworkedTag, RemoteMoveCommand>]
    [None<MovementStateComponent>] // Não processa se já houver movimento em andamento, deixa para o próximo frame.
    private void ProcessRemoteMove(in Entity entity, 
        in RemoteMoveCommand update, ref GridPositionComponent grid)
    {
        logger.LogDebug($"Recebido RemoteMoveCommand para a entidade {entity} com posição de grade {update.LastGridPosition} e direção {update.DirectionInput}.");
        
        grid.Value = update.LastGridPosition;
        ref var intent = ref World.AddOrGet<MoveIntentCommand>(entity);
        intent.Direction = update.DirectionInput;
        
        World.Remove<RemoteMoveCommand>(entity);
    }
    
    // Parte 1: Inicia o movimento. Válido para Cliente (predição) e Servidor (autoridade).
    [Query]
    [All<NetworkedTag, MoveIntentCommand, GridPositionComponent, SpeedComponent>]
    [None<MovementStateComponent>]
    private void StartMovement(in Entity entity, 
        ref DirectionComponent dir, ref GridPositionComponent gridPos, in SpeedComponent speed, 
        in MoveIntentCommand intent)
    {
        GridVector targetGridPos = gridPos.Value + intent.Direction;

        if (!gameMap.IsTileWalkable(targetGridPos))
        {
            World.Remove<MoveIntentCommand>(entity);
            logger.LogWarning("Movimento inválido na direção {Direction} do nó {Entity}.", intent.Direction, entity);
        }
        // Converte o vetor de movimento (ex: {X:1, Y:0}) para a enumeração (ex: DirectionEnum.East)
        dir.Value = intent.Direction.VectorToDirection();
            
        var startPixelPos = new WorldPosition(gridPos.Value.X * GridSize, gridPos.Value.Y * GridSize);
        var targetPixelPos = new WorldPosition(targetGridPos.X * GridSize, targetGridPos.Y * GridSize);
            
        var distance = startPixelPos.DistanceTo(targetPixelPos);
        var duration = speed.Value > 0 ? distance / speed.Value : 0f;

        // Adiciona o componente que representa o ESTADO do movimento.
        World.Add(entity, new MovementStateComponent
        {
            StartPosition = startPixelPos,
            TargetPosition = targetPixelPos,
            Duration = duration,
            TimeElapsed = 0f
        });
    }
}