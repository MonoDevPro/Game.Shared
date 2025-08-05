using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Shared.Enums;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Godot;

namespace Game.Shared.Shared.Infrastructure.ECS.Systems;

/// <summary>
/// Sistema compartilhado que move fisicamente uma entidade em direção a um alvo.
/// Roda tanto no cliente (predição) quanto no servidor (autoritativo).
/// </summary>
public partial class ProcessMovementSystem : BaseSystem<World, float>
{
    private const int GridSize = 32;

    public ProcessMovementSystem(World world) : base(world) { }

    /// <summary>
    /// Query 1: Inicia o movimento a partir de uma intenção.
    /// Converte a intenção de movimento em um alvo (TargetPositionComponent).
    /// </summary>
    [Query]
    [All<MoveIntentCommand>]
    [None<IsMovingTag>]
    private void InitiateMovement(in Entity entity, 
        ref GridPositionComponent gridPos, in MoveIntentCommand cmd, ref DirectionComponent dir)
    {
        //GD.Print(World.GetAllComponents(entity));
        
        var targetGridPos = gridPos.Value + cmd.Direction;
        var targetPixelPos = new Vector2(targetGridPos.X * GridSize, targetGridPos.Y * GridSize);
        
        // atualiza a direção do movimento
        dir.Value = VectorToDirection(cmd.Direction);
        
        World.Add(entity, new TargetPositionComponent { Value = targetPixelPos });
        World.Add<IsMovingTag>(entity);
        
        // Remove a intenção de movimento pois já executamos a ação.
        World.Remove<MoveIntentCommand>(entity);
    }
    
    /// <summary>
    /// Query 2: Executa o movimento contínuo a cada frame de física.
    /// Move a entidade em direção ao alvo e verifica se o movimento terminou.
    /// </summary>
    [Query]
    [All<IsMovingTag, SceneBodyRefComponent, SpeedComponent, TargetPositionComponent>]
    private void ExecuteMovement(
        [Data] float delta, // <-- PEDIMOS O DELTA TIME
        in Entity entity, 
        ref SceneBodyRefComponent bodyRef, 
        in SpeedComponent speed, 
        in TargetPositionComponent target)
    {
        var characterBody = bodyRef.Value;
        var targetPosition = target.Value;
        var distanceToTarget = characterBody.GlobalPosition.DistanceTo(targetPosition);
        var movementThisFrame = speed.Value * delta;

        // --- LÓGICA ANTI-OVERSHOOT ---
        // Se a nossa distância até ao alvo for menor do que o que vamos mover neste frame...
        if (distanceToTarget <= movementThisFrame)
        {
            // ...simplesmente "teleportamos" para o destino e finalizamos.
            characterBody.Velocity = Vector2.Zero;
            characterBody.GlobalPosition = targetPosition;

            ref var gridPos = ref World.Get<GridPositionComponent>(entity);
            gridPos.Value = new Vector2I((int)(targetPosition.X / GridSize), (int)(targetPosition.Y / GridSize));

            World.Remove<TargetPositionComponent>(entity);
            World.Remove<IsMovingTag>(entity);
        }
        else
        {
            // Caso contrário, continuamos o movimento normalmente.
            var direction = characterBody.GlobalPosition.DirectionTo(targetPosition);
            characterBody.Velocity = direction * speed.Value;
            characterBody.MoveAndSlide();
        }
    }
    
    // Função auxiliar para converter o vetor de input para a nossa enum de direção
    public static DirectionEnum VectorToDirection(Vector2I vector)
    {
        if (vector.Y < 0)
        {
            if (vector.X < 0) return DirectionEnum.NorthWest;
            if (vector.X > 0) return DirectionEnum.NorthEast;
            return DirectionEnum.North;
        }
        if (vector.Y > 0)
        {
            if (vector.X < 0) return DirectionEnum.SouthWest;
            if (vector.X > 0) return DirectionEnum.SouthEast;
            return DirectionEnum.South;
        }
        if (vector.X < 0) return DirectionEnum.West;
        if (vector.X > 0) return DirectionEnum.East;
        return DirectionEnum.None;
    }
}