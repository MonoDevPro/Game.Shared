using Game.Core.Common.ValueObjetcs;

namespace GameClient.Core.ECS.Components.States;

/// <summary>
/// (Apenas no cliente) Componente que gerencia a interpolação visual do movimento.
/// </summary>
public struct MovementProgressComponent
{
    public MapPosition StartPosition;
    public MapPosition TargetPosition;
    public float Duration;
    public float TimeElapsed;

    public override string ToString()
    {
        return $"MovementStateComponent(Start: {StartPosition}, Target: {TargetPosition}, Duration: {Duration}, TimeElapsed: {TimeElapsed})";
    }
}