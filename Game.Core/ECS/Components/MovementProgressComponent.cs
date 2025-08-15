using Game.Core.Entities.Common.ValueObjetcs;

namespace Game.Core.ECS.Components;

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