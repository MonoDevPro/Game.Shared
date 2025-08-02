using Godot;

namespace Game.Shared.Scripts.Shared.ECS.Components
{
    // --- Componentes de Dados Principais ---

    /// <summary>
    /// Armazena a posição lógica da entidade no mundo do jogo.
    /// Esta é a fonte da verdade para a posição.
    /// </summary>
    public struct PositionComponent { public Vector2 Value; }
    
    /// <summary>
    /// Armazena a posição LÓGICA da entidade no grid (em coordenadas de tile).
    /// Esta é a fonte da verdade no servidor.
    /// </summary>
    public struct GridPositionComponent { public Vector2I Value; }
    
    // --- Componentes de Referência de Cena ---
    
    /// <summary>
    /// Referência a um corpo físico Godot que representa a entidade no mundo 2D.
    /// Usado para sincronizar a física entre o ECS e Godot.
    /// </summary>
    public struct SceneBodyRefComponent { public CharacterBody2D Value; }

    
    // --- Componentes de Tag (Marcadores) ---

    /// <summary>
    /// Marca uma entidade como sendo sincronizada pela rede e fornece seu ID de rede.
    /// </summary>
    public struct NetworkedTag { public int Id; }

    /// <summary>
    /// Marca a entidade que é controlada diretamente pelo jogador local.
    /// Usado no cliente para aplicar predição e processar input local.
    /// </summary>
    public struct PlayerControllerTag { }

    /// <summary>
    /// Marca uma entidade que representa outro jogador conectado (um "proxy").
    /// Usado no cliente para aplicar interpolação de estado em vez de predição.
    /// </summary>
    public struct RemoteProxyTag { }
    
    /// <summary>
    /// Tag para indicar que a entidade está atualmente se movendo de um tile para outro.
    /// Usada para prevenir novos movimentos até que o atual termine.
    /// </summary>
    public struct IsMovingTag {}
}
