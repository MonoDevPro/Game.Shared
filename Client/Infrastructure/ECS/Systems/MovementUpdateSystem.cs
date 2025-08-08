using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Client.Infrastructure.Adapters;
using Game.Shared.Client.Infrastructure.ECS.Components;
using Godot;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.Math;

namespace Game.Shared.Client.Infrastructure.ECS.Systems;

/// <summary>
/// Sistema cliente que lida com as atualizações de estado do servidor.
/// - Para o jogador local, corrige a posição se houver dessincronização (reconciliação).
/// - Para jogadores remotos, inicia a interpolação de movimento (tween).
/// </summary>
public partial class MovementUpdateSystem(World world) : BaseSystem<World, float>(world)
{
    private const int GridSize = 32;
    //private const float MoveDuration = 0.3f;
    
    [Query]
    [All<GridPositionComponent, MovementUpdateCommand>]
    private void HandleMovementUpdate(in Entity entity, 
        ref GridPositionComponent gridPos, ref DirectionComponent dir, in MovementUpdateCommand update)
    {
        ref var bodyRef = ref World.Get<SceneBodyRefComponent>(entity);
        Vector2 targetVisualPos = new Vector2(update.NewGridPosition.X * GridSize, update.NewGridPosition.Y * GridSize);
        
        var direction = update.NewGridPosition - gridPos.Value;
        if (direction != GridVector.Zero)
            dir.Value = direction.VectorToDirection();
        
        gridPos.Value = update.NewGridPosition;
        
        if (World.Has<RemoteProxyTag>(entity))
        {
            // --- CÁLCULO DINÂMICO DA DURAÇÃO ---
            ref readonly var speed = ref World.Get<SpeedComponent>(entity);
            var currentPosition = bodyRef.Value.GlobalPosition;
            var distance = currentPosition.DistanceTo(targetVisualPos);

            // Evita divisão por zero se a velocidade for 0 ou a distância for mínima.
            var calculatedDuration = speed.Value > 0 && distance > 0.1f
                ? distance / speed.Value
                : 0f;
            
            ref var tween = ref World.AddOrGet<MovementTweenComponent>(entity);
            tween.StartPosition = bodyRef.Value.GlobalPosition;
            tween.TargetPosition = targetVisualPos;
            tween.Duration = calculatedDuration; // Usa a nova duração
            tween.TimeElapsed = 0f; // Reiniciamos o tempo da interpolação.
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