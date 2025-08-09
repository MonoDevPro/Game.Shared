using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Infrastructure.Adapters;
using GameClient.Infrastructure.ECS.Components;
using Godot;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Tags;
using Shared.Infrastructure.Math;

namespace GameClient.Infrastructure.ECS.Systems.Physics;

/// <summary>
/// No cliente, move fisicamente os nós Godot.
/// - Para o jogador local, executa o movimento preditivo com base no input.
/// - Para jogadores remotos, processa a interpolação suave (tween).
/// </summary>
public partial class MovementSystem(World world) : BaseSystem<World, float>(world)
{
    private const int GridSize = 32;

    // Query para iniciar o movimento preditivo do jogador local
    [Query]
    [All<PlayerControllerTag, MoveIntentCommand>]
    [None<IsMovingTag>]
    private void InitiateLocalMovement(in Entity entity, ref GridPositionComponent gridPos, in MoveIntentCommand cmd)
    {
        var targetGridPos = gridPos.Value + cmd.Direction;
        var targetPixelPos = new WorldPosition(targetGridPos.X * GridSize, targetGridPos.Y * GridSize);
        
        World.Add(entity, new TargetPositionComponent { Value = targetPixelPos });
        World.Add<IsMovingTag>(entity);
        World.Remove<MoveIntentCommand>(entity);
    }
    
    // Query que efetivamente move os nós que têm um alvo (movimento preditivo)
    [Query]
    [All<IsMovingTag, SceneBodyRefComponent, SpeedComponent, TargetPositionComponent>]
    private void ExecuteAuthoritativeMovement([Data] float delta, in Entity entity, ref SceneBodyRefComponent bodyRef, in SpeedComponent speed, in TargetPositionComponent target)
    {
        var characterBody = bodyRef.Value;
        var targetPosition = target.Value.ToGodotVector2(); // Conversão na fronteira
        
        if (characterBody.GlobalPosition.DistanceTo(targetPosition) <= speed.Value * delta)
        {
            characterBody.GlobalPosition = targetPosition;
            World.Remove<TargetPositionComponent>(entity);
            World.Remove<IsMovingTag>(entity);
        }
        else
        {
            var direction = characterBody.GlobalPosition.DirectionTo(targetPosition);
            characterBody.Velocity = direction * speed.Value;
            characterBody.MoveAndSlide();
        }
    }

    // Query que processa a interpolação suave dos jogadores remotos
    [Query]
    [All<MovementTweenComponent>]
    private void ProcessTween([Data] in float delta, in Entity entity, ref MovementTweenComponent tween)
    {
        tween.TimeElapsed += delta;
        float alpha = Mathf.Clamp(tween.TimeElapsed / tween.Duration, 0f, 1f);

        ref var bodyRef = ref World.Get<SceneBodyRefComponent>(entity);
        var startPos = tween.StartPosition; 
        var targetPos = tween.TargetPosition;
        
        bodyRef.Value.GlobalPosition = startPos.Lerp(targetPos, alpha).ToGodotVector2(); // Conversão na fronteira

        if (alpha >= 1.0f)
            World.Remove<MovementTweenComponent>(entity);
    }
}