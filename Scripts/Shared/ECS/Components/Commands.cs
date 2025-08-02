using Godot;

namespace Game.Shared.Scripts.Shared.ECS.Components;

// --- Componentes de Comando ---

/// <summary>
/// Comando do cliente para o servidor com a intenção de mover em uma direção.
/// </summary>
public struct MoveIntentCommand { public Vector2I Direction; }
    
/// <summary>
/// Comando do servidor para o cliente informando a nova posição de uma entidade no grid.
/// </summary>
public struct StateUpdateCommand
{
    public int NetId;
    public Vector2I NewGridPosition;
}
    
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