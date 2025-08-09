using GameClient.Presentation.Entities.Character.Sprites;

namespace GameClient.Infrastructure.ECS.Components;

// --- Component de Sprite do cliente ---
/// <summary>
/// Componente que armazena a referência ao sprite do personagem no cliente.
/// Usado para aplicar animações e atualizações visuais.
/// </summary>
public struct SpriteRefComponent { public CharacterSprite Value; }