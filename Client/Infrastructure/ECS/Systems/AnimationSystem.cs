using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Shared.Enums;
using Game.Shared.Shared.Infrastructure.ECS.Components;

namespace Game.Shared.Client.Infrastructure.ECS.Systems;

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