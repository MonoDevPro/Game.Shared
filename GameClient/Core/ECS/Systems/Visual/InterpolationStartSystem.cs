using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Core.Common.ValueObjetcs;
using GameClient.Core.Common;
using GameClient.Core.ECS.Components;
using GameClient.Core.ECS.Components.Visual;

namespace GameClient.Core.ECS.Systems.Visual;

/// <summary>
/// Detects changes in MapPosition and (re)starts visual interpolation towards the target world position.
/// </summary>
public sealed partial class InterpolationStartSystem : BaseSystem<World, float>
{
    public InterpolationStartSystem(World world) : base(world) {}

    [Query]
    [All<MapPositionComponent, CharNodeRefComponent>]
    [None<VisualInterpolationComponent>]
    private void StartInterpolation(in Entity entity, ref MapPositionComponent mapPos, ref CharNodeRefComponent nodeRef)
    {
        if (nodeRef.Value is null) return; // Ensure the node reference is valid
        
        var target = GridToWorld.ToWorld(mapPos.Value);
        var from = new WorldPosition(nodeRef.Value.GlobalPosition.X, nodeRef.Value.GlobalPosition.Y);
        var vip = new VisualInterpolationComponent
        {
            FromWorld = from,
            ToWorld = target,
            Elapsed = 0f,
            Duration = 0f // will be set in update system (based on speed)
        };
        World.Add(entity, vip);
    }

    [Query]
    [All<MapPositionComponent, CharNodeRefComponent, VisualInterpolationComponent>]
    private void RestartInterpolation(ref MapPositionComponent mapPos, ref CharNodeRefComponent nodeRef,
        ref VisualInterpolationComponent vip)
    {
        if (nodeRef.Value is null) 
            return;
        
        var newTarget = GridToWorld.ToWorld(mapPos.Value);
        if (!newTarget.Equals(vip.ToWorld))
        {
            vip.FromWorld = new WorldPosition(nodeRef.Value.GlobalPosition.X, nodeRef.Value.GlobalPosition.Y);
            vip.ToWorld = newTarget;
            vip.Elapsed = 0f;
            // Keep duration; will be updated by InterpolationUpdateSystem
        }
    }

    public override void Update(in float dt)
    {
        var q = new QueryDescription()
            .WithAll<MapPositionComponent, CharNodeRefComponent>()
            .WithNone<VisualInterpolationComponent>();

        // Start interpolation when not currently interpolating
        World.Query(in q, (Entity e, ref MapPositionComponent mapPos, ref CharNodeRefComponent nodeRef) =>
        {
            if (nodeRef.Value is null) return;
            var target = GridToWorld.ToWorld(mapPos.Value);
            var from = new WorldPosition(nodeRef.Value.GlobalPosition.X, nodeRef.Value.GlobalPosition.Y);
            var vip = new VisualInterpolationComponent
            {
                FromWorld = from,
                ToWorld = target,
                Elapsed = 0f,
                Duration = 0f // will be set in update system (based on speed)
            };
            World.Add(e, vip);
        });

        // If already interpolating but target changed, restart from current position
        var q2 = new QueryDescription().WithAll<MapPositionComponent, CharNodeRefComponent, VisualInterpolationComponent>();
        World.Query(in q2, (ref MapPositionComponent mapPos, ref CharNodeRefComponent nodeRef, ref VisualInterpolationComponent vip) =>
        {
            if (nodeRef.Value is null) return;
            var newTarget = GridToWorld.ToWorld(mapPos.Value);
            if (!newTarget.Equals(vip.ToWorld))
            {
                vip.FromWorld = new WorldPosition(nodeRef.Value.GlobalPosition.X, nodeRef.Value.GlobalPosition.Y);
                vip.ToWorld = newTarget;
                vip.Elapsed = 0f;
                // Keep duration; will be updated by InterpolationUpdateSystem
            }
        });
    }
}