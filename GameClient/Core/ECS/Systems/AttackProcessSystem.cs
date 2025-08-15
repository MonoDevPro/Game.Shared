using Arch.System.SourceGenerator;
using Game.Core.ECS.Components;
using Game.Core.ECS.Components.Commands;

namespace Shared.Features.Game.Player.Systems;

public partial class AttackProcessSystem(World world) : BaseSystem<World, float>(world)
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