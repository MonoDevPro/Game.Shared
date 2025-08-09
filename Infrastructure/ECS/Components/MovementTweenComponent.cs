using Shared.Infrastructure.Math;

namespace GameClient.Infrastructure.ECS.Components;

/// <summary>
/// (Apenas no cliente) Componente que gerencia a interpolação visual do movimento.
/// </summary>
public struct MovementTweenComponent
{
    public WorldPosition StartPosition;
    public WorldPosition TargetPosition;
    public float Duration;
    public float TimeElapsed;
}