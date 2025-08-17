using Game.Core.Common.Enums;
using Game.Core.Common.ValueObjetcs;

namespace GameServer.Infrastructure.EfCore.Worker.Models;

public class CharacterSaveModel
{
    public int CharacterId { get; init; }
    public int AccountId { get; init; }
    public string Name { get; init; } = string.Empty;
    public VocationEnum Vocation { get; init; }
    public GenderEnum Gender { get; init; }
    public DirectionEnum Direction { get; init; }
    public MapPosition Position { get; init; }
    public float Speed { get; init; }
}

public class CharacterLoadModel
{
    public int CharacterId { get; init; }
    public int AccountId { get; init; }
    public string Name { get; init; } = string.Empty;
    public VocationEnum Vocation { get; init; }
    public GenderEnum Gender { get; init; }
    public DirectionEnum Direction { get; init; }
    public MapPosition Position { get; init; }
    public float Speed { get; init; }
}
