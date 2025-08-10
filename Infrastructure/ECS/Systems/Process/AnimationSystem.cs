using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Infrastructure.ECS.Components;
using Shared.Core.Enums;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Tags;

namespace GameClient.Infrastructure.ECS.Systems.Process;

public partial class AnimationSystem(World world) : BaseSystem<World, float>(world)
{
    // Query para TODOS os personagens em movimento (local e remoto)
    [Query]
    [All<MovementStateComponent, SpriteRefComponent, DirectionComponent>]
    private void UpdateWalkingAnimations(in SpriteRefComponent spriteRef, in DirectionComponent direction, in SpeedComponent speed)
    {
        spriteRef.Value.SetState(ActionEnum.Walk, direction.Value, speed.Value);
    }

    // Query para TODOS os personagens que estão PARADOS.
    [Query]
    [All<SpriteRefComponent, DirectionComponent>]
    [None<MovementStateComponent>]
    private void UpdateIdleAnimations(in SpriteRefComponent spriteRef, in DirectionComponent direction, in SpeedComponent speed)
    {
        spriteRef.Value.SetState(ActionEnum.Idle, direction.Value, speed.Value);
    }
}