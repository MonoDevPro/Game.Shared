using Godot;

namespace Game.Shared.Client.Infrastructure.ECS.Components;

public struct SceneBodyRefComponent {
    /// <summary>
    /// Referência para o corpo da cena associado a esta entidade.
    /// </summary>
    public CharacterBody2D Value;
}