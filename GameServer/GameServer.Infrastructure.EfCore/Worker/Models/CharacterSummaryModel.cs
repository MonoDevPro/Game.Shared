using Game.Core.Common.Enums;

namespace GameServer.Infrastructure.EfCore.Worker.Models;

public sealed class CharacterSummaryModel
{
    public int CharacterId { get; init; }
    public string Name { get; init; } = string.Empty;
    public VocationEnum Vocation { get; init; }
    public GenderEnum Gender { get; init; }
}
