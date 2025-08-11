using Arch.System.SourceGenerator;
using Shared.Infrastructure.ECS.Commands;
using Shared.Infrastructure.ECS.Components;

namespace Shared.Infrastructure.ECS.Systems;

public partial class AttackProcessSystem(World world) : BaseSystem<World, float>(world)
{
    [Query]
    [All<AttackIntentCommand>]
    [None<AttackStateComponent>]
    private void ProcessAttackIntent(in Entity entity, ref AttackIntentCommand attackIntent)
    {
        World.Add(entity, new AttackStateComponent
        {
            Direction = attackIntent.Direction,
            Duration = 0.5f,
            TimeElapsed = 0f
        });
        
        World.Remove<AttackIntentCommand>(entity);
    }
    
    [Query]
    [All<AttackStateComponent>]
    [None<AttackIntentCommand>]
    private void Update([Data] in float delta, in Entity entity, ref AttackStateComponent attackState)
    {
        attackState.TimeElapsed += delta;
        if (attackState.TimeElapsed >= attackState.Duration)
            World.Remove<AttackStateComponent>(entity);
    }
}