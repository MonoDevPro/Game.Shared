using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Server.Headless.Core.ECS.Game.Components.Commands;
using Game.Server.Headless.Core.ECS.Game.Components.States;

namespace Game.Server.Headless.Core.ECS.Game.Systems;

public partial class AttackSystem(World world) : BaseSystem<World, float>(world)
{
    [Query]
    [All<AttackIntentCommand>]
    [None<AttackProgressComponent>]
    private void ProcessAttackIntent(in Entity entity, ref AttackIntentCommand attackIntent)
    {
        World.Add(entity, new AttackProgressComponent
        {
            Direction = attackIntent.Direction,
            Duration = 0.5f,
            TimeElapsed = 0f
        });
        
        World.Remove<AttackIntentCommand>(entity);
    }
    
    [Query]
    [All<AttackProgressComponent>]
    [None<AttackIntentCommand>]
    private void Update([Data] in float delta, in Entity entity, ref AttackProgressComponent attackState)
    {
        attackState.TimeElapsed += delta;
        if (attackState.TimeElapsed >= attackState.Duration)
            World.Remove<AttackProgressComponent>(entity);
    }
}