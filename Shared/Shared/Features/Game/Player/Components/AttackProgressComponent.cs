using Shared.Core.Common.Math;

namespace Shared.Features.Game.Character.Components;

public struct AttackProgressComponent
{
    public GridVector Direction;
    public float Duration;
    public float TimeElapsed;
}