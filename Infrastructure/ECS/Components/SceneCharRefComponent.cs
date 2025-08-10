using Godot;

namespace GameClient.Infrastructure.ECS.Components;

public struct SceneCharRefComponent {
    /// <summary>
    /// Referência para o corpo da cena associado a esta entidade.
    /// </summary>
    public Node2D Value;
}