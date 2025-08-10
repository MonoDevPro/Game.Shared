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


    /*[Query]
    [All<MoveIntentCommand, GridPositionComponent, SpeedComponent>]
    [None<MovementStateComponent>] // Garante que só processa se não estiver se movendo
    private void ValidateMoveIntent(in Entity entity,
        ref GridPositionComponent gridPos, in MoveIntentCommand intent)
    {
        GridVector targetGridPos = gridPos.Value + intent.Direction;

        // --- LÓGICA DE VALIDAÇÃO (Exemplo) ---
        if (gameMap.IsTileWalkable(targetGridPos))
        {
            var targetPixelPos = new WorldPosition(targetGridPos.X * GridSize, targetGridPos.Y * GridSize);
            World.Add(entity, new TargetPositionComponent { Value = targetPixelPos });
            World.Add<IsMovingTag>(entity);
        }
        else
            logger.LogWarning("Movimento inválido na direção {Direction}.", intent.Direction);

        // Remove a intenção de movimento, pois já foi processada
        World.Remove<MoveIntentCommand>(entity);
    }

    // Query que efetivamente move os nós que têm um alvo (movimento preditivo)
    [Query]
    [All<IsMovingTag, TargetPositionComponent>]
    [None<MovementStateComponent>] // Garante que não está interpolando
    private void StartMovement([Data] float delta, in Entity entity,
        ref WorldPositionComponent position, in SpeedComponent speed, in TargetPositionComponent target)
    {
        var worldPosition = position.Value;
        var targetWorldPosition = target.Value;

        // Calcula a duração com base na distância e velocidade
        var distance = worldPosition.DistanceTo(targetWorldPosition);
        var duration = speed.Value > 0 && distance > 0.1f ? distance / speed.Value : 0f;

        // Se a duração é zero ou negativa, ou se já está perto o suficiente, move imediatamente
        if (duration <= 0f || worldPosition.DistanceTo(targetWorldPosition) <= speed.Value * delta)
        {
            // Se a duração é zero ou negativa, mova imediatamente
            position.Value = targetWorldPosition;
            World.Remove<TargetPositionComponent>(entity);
            World.Remove<IsMovingTag>(entity);
            return;
        }

        // Adiciona o componente de tween
        World.Add(entity, new MovementStateComponent
        {
            StartPosition = worldPosition,
            TargetPosition = targetWorldPosition,
            Duration = duration,
            TimeElapsed = 0f
        });
    }

    // Query que processa a interpolação suave dos jogadores remotos
    [Query]
    [All<MovementStateComponent, WorldPositionComponent>]
    private void ProcessTween([Data] in float delta, in Entity entity, ref MovementStateComponent state, ref WorldPositionComponent position)
    {
        state.TimeElapsed += delta;

        float alpha = System.Math.Clamp(state.TimeElapsed / state.Duration, 0f, 1f);

        var startPos = state.StartPosition;
        var targetPos = state.TargetPosition;

        position.Value = startPos.Lerp(targetPos, alpha);

        if (alpha >= 1.0f)
            World.Remove<MovementStateComponent>(entity);
    }*/
}