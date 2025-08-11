using Shared.Infrastructure.Math;

namespace Shared.Infrastructure.ECS.Components;

public struct AttackStateComponent
{
    public GridVector Direction;
    public float Duration;
    public float TimeElapsed;
}