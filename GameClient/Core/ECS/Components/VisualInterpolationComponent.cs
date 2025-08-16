using Game.Core.Entities.Common.ValueObjetcs;

namespace GameClient.Core.ECS.Components;

/// <summary>
/// Client-only: tracks an in-flight pixel-space interpolation from FromWorld to ToWorld.
/// </summary>
public struct VisualInterpolationComponent
{
    public WorldPosition FromWorld;
    public WorldPosition ToWorld;
    public float Elapsed;
    public float Duration;
}
