using Godot;

namespace Game.Shared.Client.Infrastructure.ECS.Components;

/// <summary>
/// (Apenas no cliente) Componente que gerencia a interpolação visual do movimento.
/// </summary>
public struct MovementTweenComponent
{
    public Vector2 StartPosition;
    public Vector2 TargetPosition;
    public float Duration;
    public float TimeElapsed;
}