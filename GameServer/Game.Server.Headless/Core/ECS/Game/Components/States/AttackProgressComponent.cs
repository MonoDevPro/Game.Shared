using Game.Core.Common.ValueObjetcs;

namespace Game.Server.Headless.Core.ECS.Game.Components.States;

public struct AttackProgressComponent
{
    public MapPosition Direction;
    public float Duration;
    public float TimeElapsed;
}