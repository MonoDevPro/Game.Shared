using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Core.ECS.Components;
using Shared.Core.Common.Enums;
using Shared.Features.Game.Character.Components;

namespace GameClient.Core.ECS.Systems;

public partial class AnimationSystem(World world) : BaseSystem<World, float>(world)
{
    // Query para TODOS os personagens em movimento (local e remoto)
    [Query]
    [All<MovementProgressComponent, CharSpriteRefComponent, DirectionComponent>]
    private void UpdateWalkingAnimations(in CharSpriteRefComponent charSpriteRef, in DirectionComponent direction, in SpeedComponent speed)
    {
        charSpriteRef.Value.SetState(ActionEnum.Walk, direction.Value, speed.Value);
    }

    // Query para TODOS os personagens que estão PARADOS.
    [Query]
    [All<CharSpriteRefComponent, DirectionComponent>]
    [None<MovementProgressComponent>]
    private void UpdateIdleAnimations(in CharSpriteRefComponent charSpriteRef, in DirectionComponent direction, in SpeedComponent speed)
    {
        charSpriteRef.Value.SetState(ActionEnum.Idle, direction.Value, speed.Value);
    }
    
    // Query para TODOS os personagens que estão ATACANDO.
    [Query]
    [All<AttackProgressComponent, CharSpriteRefComponent, DirectionComponent>]
    private void UpdateAttackAnimations(in CharSpriteRefComponent charSpriteRef, in DirectionComponent direction, in SpeedComponent speed)
    {
        charSpriteRef.Value.SetState(ActionEnum.Attack, direction.Value, speed.Value);
    }
}