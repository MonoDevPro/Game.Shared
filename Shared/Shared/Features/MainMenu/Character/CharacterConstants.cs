namespace Shared.Features.MainMenu.Character;

public class CharacterConstants
{
    public const int MinCharacterNameLength = 3;
    public const int MaxCharacterNameLength = 30;
    public const int MaxCharacterCount = 5;
    // Letters and digits with single spaces between words, no leading/trailing spaces
    public const string NameRegexPattern = @"^(?! )[A-Za-z0-9]+(?: [A-Za-z0-9]+)*(?<! )$";
}