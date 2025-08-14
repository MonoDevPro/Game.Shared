using Shared.Core.Common.Math;

namespace Shared.Features.Game.Character.Components;

/// <summary>
/// (Apenas no cliente) Componente que gerencia a interpolação visual do movimento.
/// </summary>
public struct MovementProgressComponent
{
    public WorldPosition StartPosition;
    public WorldPosition TargetPosition;
    public float Duration;
    public float TimeElapsed;

    public override string ToString()
    {
        return $"MovementStateComponent(Start: {StartPosition}, Target: {TargetPosition}, Duration: {Duration}, TimeElapsed: {TimeElapsed})";
    }
}