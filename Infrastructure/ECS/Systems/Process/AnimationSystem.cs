using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Infrastructure.ECS.Components;
using Shared.Core.Enums;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Tags;

namespace GameClient.Infrastructure.ECS.Systems.Process;

public partial class AnimationSystem : BaseSystem<World, float>
{
    public AnimationSystem(World world) : base(world) { }

    // Query para TODOS os personagens em movimento (local e remoto)
    [Query]
    [Any<MovementTweenComponent, IsMovingTag>]
    [All<SpriteRefComponent, DirectionComponent>]
    private void UpdateWalkingAnimations(in SpriteRefComponent spriteRef, in DirectionComponent direction, in SpeedComponent speed)
    {
        spriteRef.Value.SetState(ActionEnum.Walk, direction.Value, speed.Value);
    }

    // Query para TODOS os personagens que estão PARADOS.
    [Query]
    [All<SpriteRefComponent, DirectionComponent, SpeedComponent>]
    [None<IsMovingTag, MovementTweenComponent>]
    private void UpdateIdleAnimations(in SpriteRefComponent spriteRef, in DirectionComponent direction, in SpeedComponent speed)
    {
        // Para a animação de "parado", a velocidade de movimento não importa,
        // então passamos a velocidade base para que a escala seja 1.0.
        spriteRef.Value.SetState(ActionEnum.Idle, direction.Value, speed.Value);
    }
}