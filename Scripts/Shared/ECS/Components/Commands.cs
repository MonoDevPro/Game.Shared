using Godot;

namespace Game.Shared.Scripts.Shared.ECS.Components;

// --- Componentes de Comando ---

/// <summary>
/// Um comando temporário que representa uma requisição de input a ser processada.
/// É criado pelo sistema de rede (no servidor) ou pelo sistema de input local (no cliente).
/// </summary>
public struct InputRequestCommand { public Vector2 Value; }
    
/// <summary>
/// Um comando que carrega o estado autoritativo do servidor para uma entidade.
/// Usado pelo ReconciliationSystem no jogador local.
/// </summary>
public struct AuthoritativeStateCommand
{
    public Vector2 Position;
    public Vector2 Velocity;
}
    
/// <summary>
/// Um componente que guarda os dados necessários para interpolar suavemente
/// a posição de uma entidade remota.
/// </summary>
public struct InterpolationDataComponent
{
    public Vector2 StartPosition;
    public Vector2 TargetPosition;
    public float TimeElapsed;
}
