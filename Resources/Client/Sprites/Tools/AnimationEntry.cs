using Game.Shared.Scripts.Shared.Enums;
using Godot;

namespace GodotFloorLevels.Scripts.Resources.Tools;

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
