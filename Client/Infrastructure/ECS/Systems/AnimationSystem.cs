using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Client.Presentation.Entities.Character.Sprites;
using Game.Shared.Shared.Enums;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Godot;

namespace Game.Shared.Client.Infrastructure.ECS.Systems;

/// <summary>
/// Sistema que lê o estado de movimento (se está em tween) para atualizar a animação.
/// </summary>
public partial class AnimationSystem : BaseSystem<World, float>
{
    public AnimationSystem(World world) : base(world) { }

    // Query para personagens que estão ATUALMENTE se movendo.
    [Query]
    [All<SceneBodyRefComponent, MovementTweenComponent>]
    private void UpdateWalkingAnimations(in Entity entity, ref SceneBodyRefComponent body, ref MovementTweenComponent tween)
    {
        var sprite = body.Value.GetNodeOrNull<CharacterSprite>("CharacterSprite");
        if (sprite == null) return;

        // Se tem o componente de tween, a ação é sempre "Walk".
        var action = ActionEnum.Walk;
            
        // A direção é calculada a partir do início e do fim do movimento.
        var direction = PositionsToDirection(tween.StartPosition, tween.TargetPosition);

        sprite.SetState(action, direction);
    }

    // Query para personagens que estão PARADOS.
    [Query]
    [All<SceneBodyRefComponent>]
    [None<MovementTweenComponent>] // Roda apenas se NÃO estiver em movimento.
    private void UpdateIdleAnimations(in Entity entity, ref SceneBodyRefComponent body)
    {
        var sprite = body.Value.GetNodeOrNull<CharacterSprite>("CharacterSprite");
        if (sprite == null) return;

        // Se não está se movendo, a ação é "Idle".
        var action = ActionEnum.Idle;
        // A direção é mantida a mesma de antes para que o personagem continue olhando para o último lado.
        var direction = sprite.Direction; 

        sprite.SetState(action, direction);
    }

    /// <summary>
    /// Calcula a direção do movimento com base na posição inicial e final.
    /// </summary>
    private DirectionEnum PositionsToDirection(Vector2 start, Vector2 end)
    {
        Vector2 moveVector = (end - start).Normalized();
            
        // Lógica reaproveitada para converter um vetor em uma direção Enum.
        if (moveVector.IsZeroApprox()) return DirectionEnum.None;

        var angle = Mathf.RadToDeg(moveVector.Angle());

        if (angle < 0) angle += 360;

        if (angle >= 337.5 || angle < 22.5) return DirectionEnum.East;
        if (angle >= 22.5 && angle < 67.5) return DirectionEnum.SouthEast;
        if (angle >= 67.5 && angle < 112.5) return DirectionEnum.South;
        if (angle >= 112.5 && angle < 157.5) return DirectionEnum.SouthWest;
        if (angle >= 157.5 && angle < 202.5) return DirectionEnum.West;
        if (angle >= 202.5 && angle < 247.5) return DirectionEnum.NorthWest;
        if (angle >= 247.5 && angle < 292.5) return DirectionEnum.North;
        if (angle >= 292.5 && angle < 337.5) return DirectionEnum.NorthEast;

        return DirectionEnum.None;
    }
}