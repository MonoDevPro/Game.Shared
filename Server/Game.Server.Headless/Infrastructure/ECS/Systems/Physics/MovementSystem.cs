using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Shared.Infrastructure.ECS.Components;
using Shared.Infrastructure.ECS.Tags;
using Shared.Infrastructure.Math;

namespace Game.Server.Headless.Infrastructure.ECS.Systems.Physics;

public partial class MovementSystem(World world) : BaseSystem<World, float>(world)
{
    [Query]
    [All<TargetPositionComponent>] // Usamos o TargetPositionComponent como gatilho
    private void ExecuteMovement(in Entity entity, ref GridPositionComponent gridPos, in TargetPositionComponent target)
    {
        // A "física" no servidor agora é uma simples atribuição.
        // O movimento visual é problema do cliente.
        gridPos.Value = new GridVector((int)target.Value.X / 32, (int)target.Value.Y / 32);

        // Limpamos os componentes de movimento
        World.Remove<TargetPositionComponent>(entity);
        World.Remove<IsMovingTag>(entity);
    }
}