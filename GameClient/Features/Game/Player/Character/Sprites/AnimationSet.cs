using System.Collections.Generic;
using Game.Core.Entities.Common.Enums;
using Godot;

namespace GameClient.Features.Game.Player.Character.Sprites;

/// <summary>
/// Holds a collection of AnimationEntry and builds a lookup for fast access.
/// </summary>
[Tool]
public partial class AnimationSet : Resource
{
    [Export]
    public AnimationEntry[] Entries { get; set; } = [];

    private Dictionary<(ActionEnum, DirectionEnum), StringName> _lookup;
    
    public void _Ready()
    {
        // Initialize the lookup dictionary
        _lookup = new Dictionary<(ActionEnum, DirectionEnum), StringName>(Entries.Length);
        
        foreach (var entry in Entries)
        {
            var key = (entry.State, entry.Direction);
            _lookup[key] = entry.AnimationName;
            
            GD.Print($"Registered animation: {entry.State}_{entry.Direction} -> {entry.AnimationName}");
        }
    }

    /// <summary>
    /// Retrieves the animation name for the given state and direction.
    /// Throws if no entry found.
    /// </summary>
    public StringName GetAnimation(ActionEnum state, DirectionEnum dir)
    {
        var key = (state, dir);
        if (_lookup != null && _lookup.TryGetValue(key, out var name))
            return name;

        throw new KeyNotFoundException($"No animation registered for {state}_{dir}");
    }
}
