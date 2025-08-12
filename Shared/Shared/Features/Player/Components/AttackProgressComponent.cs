using Shared.Infrastructure.Math;

namespace Shared.Features.Player.Components;

public struct AttackProgressComponent
{
    public GridVector Direction;
    public float Duration;
    public float TimeElapsed;
}