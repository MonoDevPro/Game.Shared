using Godot;

namespace GameClient.Core.ECS.Components;

public struct CharNodeRefComponent {
    /// <summary>
    /// Referência para o corpo da cena associado a esta entidade.
    /// </summary>
    public Node2D Value;
}