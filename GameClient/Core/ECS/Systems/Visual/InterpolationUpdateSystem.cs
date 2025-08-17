using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameClient.Core.ECS.Components;
using GameClient.Core.ECS.Components.Visual;

namespace GameClient.Core.ECS.Systems.Visual;

/// <summary>
/// Advances pixel-space interpolation each frame and applies Node2D positions.
/// </summary>
public sealed partial class InterpolationUpdateSystem : BaseSystem<World, float>
{
    public InterpolationUpdateSystem(World world) : base(world) {}
    
    [Query]
    [All<CharNodeRefComponent, VisualInterpolationComponent>]
    private void UpdateInterpolation([Data] in float delta, in Entity entity, ref CharNodeRefComponent nodeRef, ref VisualInterpolationComponent vip)
    {
        
        var toRemove = new List<Entity>(64);
        
        if (nodeRef.Value is null)
        {
            toRemove.Add(entity);
            return;
        }

        // TODO: incorporate Movement speed if available on client
        if (vip.Duration <= 0f)
        {
            // default based on one tile move at ~6 tiles/sec
            vip.Duration = 1f / 6f;
        }

        vip.Elapsed += delta;
        var t = vip.Duration <= 0f ? 1f : MathF.Min(1f, vip.Elapsed / vip.Duration);
        var pos = vip.FromWorld.Lerp(vip.ToWorld, t);
        nodeRef.Value.GlobalPosition = new Godot.Vector2(pos.X, pos.Y);

        if (t >= 1f)
        {
            nodeRef.Value.GlobalPosition = new Godot.Vector2(vip.ToWorld.X, vip.ToWorld.Y);
            toRemove.Add(entity);
        }
        
        foreach (var e in toRemove)
            World.Remove<VisualInterpolationComponent>(e);
            
    }

    public override void Update(in float dt)
    {
        var q = new QueryDescription().WithAll<CharNodeRefComponent, VisualInterpolationComponent>();
        var delta = dt; // capture copy for lambda
        var toRemove = new List<Entity>(64);

        World.Query(in q, (Entity e, ref CharNodeRefComponent nodeRef, ref VisualInterpolationComponent vip) =>
        {
            
        });

        
    }
}