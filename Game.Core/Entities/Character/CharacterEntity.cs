using Game.Core.Common;
using Game.Core.Common.Enums;
using Game.Core.Common.ValueObjetcs;

namespace Game.Core.Entities.Character;

public sealed class CharacterEntity : BaseEntity
{
    public int AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public VocationEnum Vocation { get; set; }
    public GenderEnum Gender { get; set; }
    public DirectionEnum Direction { get; set; }
    public MapPosition Position { get; set; }
    public float Speed { get; set; }
    
    // Navigation property for Account
    public Account.AccountEntity AccountEntity { get; init; } = null!;
}