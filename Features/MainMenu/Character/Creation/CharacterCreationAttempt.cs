using Shared.Core.Common.Enums;

namespace GameClient.Features.MainMenu.Character.Creation;

public struct CharacterCreationAttempt
{
    public string Name;
    public VocationEnum Vocation;
    public GenderEnum Gender;
}