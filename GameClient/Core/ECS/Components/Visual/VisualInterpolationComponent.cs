using Game.Core.Common.ValueObjetcs;

namespace GameClient.Core.ECS.Components.Visual;

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