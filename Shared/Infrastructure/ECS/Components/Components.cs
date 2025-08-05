using Game.Shared.Client.Presentation.Entities.Character.Sprites;
using Game.Shared.Shared.Enums;
using Godot;

namespace Game.Shared.Shared.Infrastructure.ECS.Components
{
    // --- Componentes de Dados Principais ---

    public struct PlayerInfoComponent
    {
        public string Name;
        public VocationEnum Vocation;
        public GenderEnum Gender;
    }
    
    /// <summary>
    /// Armazena a posição LÓGICA da entidade no grid (em coordenadas de tile).
    /// Esta é a fonte da verdade no servidor.
    /// </summary>
    public struct GridPositionComponent { public Vector2I Value; }
    
    /// <summary>
    /// Armazena a velocidade de movimento da entidade em pixels por segundo.
    /// </summary>
    public struct SpeedComponent { public float Value; }
    
    /// <summary>
    /// Armazena a direção atual do movimento da entidade.
    /// </summary>
    public struct DirectionComponent { public DirectionEnum Value; }
    
    // --- Componentes de Referência de Cena ---
    
    /// <summary>
    /// Referência a um corpo físico Godot que representa a entidade no mundo 2D.
    /// Usado para sincronizar a física entre o ECS e Godot.
    /// </summary>
    public struct SceneBodyRefComponent { public CharacterBody2D Value; }
    
    // --- Component de Sprite do cliente ---
    /// <summary>
    /// Componente que armazena a referência ao sprite do personagem no cliente.
    /// Usado para aplicar animações e atualizações visuais.
    /// </summary>
    public struct SpriteRefComponent { public CharacterSprite Value; }

    
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
    
    /// <summary>
    /// (Apenas no servidor) Componente que armazena o alvo do movimento em pixels.
    /// </summary>
    public struct TargetPositionComponent { public Vector2 Value; }
}
