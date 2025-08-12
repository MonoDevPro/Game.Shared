
using GameClient.Features.Player.Character.Sprites;

namespace GameClient.Core.ECS.Components;

// --- Component de Sprite do cliente ---
/// <summary>
/// Componente que armazena a referência ao sprite do personagem no cliente.
/// Usado para aplicar animações e atualizações visuais.
/// </summary>
public struct CharSpriteRefComponent { public CharacterSprite Value; }