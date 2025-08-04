using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Game.Shared.Shared.Infrastructure.ECS.Systems;
using Godot;

namespace Game.Shared.Client.Infrastructure.ECS.Systems;

/// <summary>
/// Sistema cliente que lida com as atualizações de estado do servidor.
/// - Para o jogador local, corrige a posição se houver dessincronização (reconciliação).
/// - Para jogadores remotos, inicia a interpolação de movimento (tween).
/// </summary>
public partial class MovementUpdateSystem(World world) : BaseSystem<World, float>(world)
{
    private const int GridSize = 32;
    private const float MoveDuration = 0.3f;
    
    [Query]
    [All<GridPositionComponent, MovementUpdateCommand>]
    private void HandleMovementUpdate(in Entity entity, 
        ref GridPositionComponent gridPos, ref DirectionComponent dir, in MovementUpdateCommand update)
    {
        ref var bodyRef = ref World.Get<SceneBodyRefComponent>(entity);
        Vector2 targetVisualPos = new Vector2(update.NewGridPosition.X * GridSize, update.NewGridPosition.Y * GridSize);
        
        var direction = update.NewGridPosition - gridPos.Value;
        if (direction != Vector2I.Zero)
            dir.Value = ProcessMovementSystem.VectorToDirection(direction);
        
        gridPos.Value = update.NewGridPosition;
        
        if (World.Has<RemoteProxyTag>(entity))
        {
            // --- A CORREÇÃO ESTÁ AQUI ---
            // Se o personagem já estiver no meio de uma interpolação...
            if (World.Has<MovementTweenComponent>(entity))
            {
                // ...removemos o componente antigo para cancelar o movimento anterior.
                World.Remove<MovementTweenComponent>(entity);
            }

            // Agora, com a certeza de que não há outra interpolação a decorrer,
            // iniciamos a nova a partir da posição visual exata em que o personagem se encontra.
            World.Add(entity, new MovementTweenComponent
            {
                StartPosition = bodyRef.Value.GlobalPosition,
                TargetPosition = targetVisualPos,
                Duration = MoveDuration,
                TimeElapsed = 0f
            });
        }
        
        World.Remove<MovementUpdateCommand>(entity);
    }
    
    // A query ProcessTween permanece exatamente a mesma.
    [Query]
    [All<MovementTweenComponent>]
    private void ProcessTween([Data] in float delta, in Entity entity, ref MovementTweenComponent tween)
    {
        tween.TimeElapsed += delta;
        float alpha = Mathf.Clamp(tween.TimeElapsed / tween.Duration, 0f, 1f);

        ref var bodyRef = ref World.Get<SceneBodyRefComponent>(entity);
        bodyRef.Value.GlobalPosition = tween.StartPosition.Lerp(tween.TargetPosition, alpha);

        if (alpha >= 1.0f)
        {
            World.Remove<MovementTweenComponent>(entity);
        }
    }
}