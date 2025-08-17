using Game.Core.Common.ValueObjetcs;

namespace GameClient.Core.ECS.Components.States;

public struct AttackProgressComponent
{
    public MapPosition Direction;
    public float Duration;
    public float TimeElapsed;
}