using Game.Core.Entities.Common.ValueObjetcs;

namespace Game.Core.ECS.Components;

public struct AttackProgressComponent
{
    public MapPosition Direction;
    public float Duration;
    public float TimeElapsed;
}