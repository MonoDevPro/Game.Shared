using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Shared.Client.Presentation.Entities.Character.Sprites;
using Game.Shared.Shared.Enums;
using Game.Shared.Shared.Infrastructure.ECS.Components;
using Godot;

namespace Game.Shared.Client.Infrastructure.ECS.Systems;

public partial class AnimationSystem : BaseSystem<World, float>
{
    public AnimationSystem(World world) : base(world) { }

    // Query para TODOS os personagens em movimento (local e remoto)
    [Query]
    [All<IsMovingTag, SceneBodyRefComponent, DirectionComponent>]
    private void UpdateWalkingAnimations(ref SpriteRefComponent spriteRef, ref DirectionComponent direction)
    {
        spriteRef.Value.SetState(ActionEnum.Walk, direction.Value);
    }

    // Query para a interpolação de jogadores REMOTOS
    [Query]
    [All<RemoteProxyTag, SceneBodyRefComponent, MovementTweenComponent>]
    private void UpdateRemoteWalkingAnimations(ref SpriteRefComponent spriteRef, ref DirectionComponent direction)
    {
        spriteRef.Value.SetState(ActionEnum.Walk, direction.Value);
    }

    // Query para TODOS os personagens que estão PARADOS.
    [Query]
    [All<SceneBodyRefComponent>]
    [None<IsMovingTag, MovementTweenComponent>] // Se não está em movimento predito NEM em interpolação
    private void UpdateIdleAnimations(ref SpriteRefComponent spriteRef, in DirectionComponent direction)
    {
        // Se o personagem não está se movendo, define o estado de idle.
        spriteRef.Value.SetState(ActionEnum.Idle, direction.Value);
    }
}