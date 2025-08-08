using Godot;
using Shared.Core.Enums;

namespace Game.Shared.Client.Presentation.Entities.Character.Sprites;

/// <summary>
/// Entry to map a combination of ActionEnum + DirectionEnum to an animation name.
/// </summary>
[Tool]
public partial class AnimationEntry : Resource
{
    [Export] public ActionEnum State { get; set; }
    [Export] public DirectionEnum Direction { get; set; }
    [Export] public StringName AnimationName { get; set; }
}
