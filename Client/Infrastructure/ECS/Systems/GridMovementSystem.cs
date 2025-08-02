using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Godot;

namespace Game.Shared.Client.Infrastructure.ECS.Systems;

public partial class GridMovementSystem(World world) : BaseSystem<World, float>(world)
{
    private const int GridSize = 32;
    private const float MoveDuration = 0.3f; // Duração do "passo"

    // Query para processar atualizações do servidor
    [Query]
    [All<GridPositionComponent, StateUpdateCommand>]
    private void HandleStateUpdate(in Entity entity, ref GridPositionComponent gridPos, in StateUpdateCommand update)
    {
        // Pega a posição visual atual do corpo.
        ref var bodyRef = ref World.Get<SceneBodyRefComponent>(entity);
        Vector2 startVisualPos = bodyRef.Value.GlobalPosition;

        // Define a nova posição lógica do grid.
        gridPos.Value = update.NewGridPosition;
        
        // Calcula a posição visual de destino.
        Vector2 targetVisualPos = new Vector2(gridPos.Value.X * GridSize, gridPos.Value.Y * GridSize);
        
        // Adiciona o componente de tween para iniciar a interpolação.
        World.Add(entity, new MovementTweenComponent
        {
            StartPosition = startVisualPos,
            TargetPosition = targetVisualPos,
            Duration = MoveDuration,
            TimeElapsed = 0f
        });

        // Remove o comando de atualização.
        World.Remove<StateUpdateCommand>(entity);
    }
    
    // Query para executar a interpolação a cada frame
    [Query]
    [All<MovementTweenComponent>]
    private void ProcessTween([Data] in float delta, in Entity entity, ref MovementTweenComponent tween)
    {
        tween.TimeElapsed += delta;
        float alpha = Mathf.Clamp(tween.TimeElapsed / tween.Duration, 0f, 1f);

        ref var bodyRef = ref World.Get<SceneBodyRefComponent>(entity);
        bodyRef.Value.GlobalPosition = tween.StartPosition.Lerp(tween.TargetPosition, alpha);

        // Se a interpolação terminou.
        if (alpha >= 1.0f)
        {
            // Remove o componente de tween e a tag que bloqueia movimento.
            World.Remove<MovementTweenComponent>(entity);
            World.Remove<IsMovingTag>(entity);
        }
    }
}