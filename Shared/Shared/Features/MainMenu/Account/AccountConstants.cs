namespace Shared.Features.MainMenu.Account;

public class AccountConstants
{
    public const int MinUsernameLength = 3;
    public const int MaxUsernameLength = 30;
    public const int MinPasswordLength = 6;
    public const int MaxPasswordLength = 30;
    public const int MaxEmailLength = 100;

    public const string UsernameRegexPattern = @"^[a-zA-Z0-9_]+$";
    public const string PasswordRegexPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]{6,30}$";
    public const string EmailRegexPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
}