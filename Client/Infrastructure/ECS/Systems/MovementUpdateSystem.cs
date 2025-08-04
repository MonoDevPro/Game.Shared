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
            // Atualiza a direção do movimento com base na nova posição
            dir.Value = ProcessMovementSystem.VectorToDirection(direction);
        
        // --- RECONCILIAÇÃO --- // --- INTERPOLAÇÃO ---
        // 1. Atualiza a posição lógica para a posição autoritativa do servidor.
        gridPos.Value = update.NewGridPosition;
        
        // REMOTO -> Atualiza a posição visual do corpo do personagem para a nova posição
        if (World.Has<RemoteProxyTag>(entity))
        {
            // 2. Inicia um tween para mover o personagem suavemente da posição
            //    antiga para a nova, criando um movimento fluido.
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
    
    // Query para executar o tween de interpolação dos jogadores remotos.
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