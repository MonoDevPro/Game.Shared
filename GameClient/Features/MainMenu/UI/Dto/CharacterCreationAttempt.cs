using Game.Core.Common.Enums;

namespace GameClient.Features.MainMenu.UI.Dto;

public readonly struct CharacterCreationAttempt
{
    public string Name { get; init; }
    public VocationEnum Vocation { get; init; }
    public GenderEnum Gender { get; init; }
}
