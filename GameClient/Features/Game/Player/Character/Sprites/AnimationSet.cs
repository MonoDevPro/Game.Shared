using Game.Core.Common.Enums;
using Godot;

namespace GameClient.Features.Game.Player.Character.Sprites;

/// <summary>
/// Gera nomes de animação seguindo a convenção "Action_Direction" (ex: Walk_North).
/// Antes usava uma lista de AnimationEntry, mas agora os nomes são diretos no SpriteFrames.
/// </summary>
[Tool]
public partial class AnimationSet : Resource
{
    // Separador configurável caso queira mudar no futuro (default "_").
    [Export] public string Separator { get; set; } = "_";

    /// <summary>
    /// Retorna o nome da animação baseado apenas na convenção.
    /// </summary>
    public StringName GetAnimation(ActionEnum state, DirectionEnum dir)
    {
        return new StringName(state.ToString().ToLower() + Separator + dir.ToString().ToLower());
    }
}
