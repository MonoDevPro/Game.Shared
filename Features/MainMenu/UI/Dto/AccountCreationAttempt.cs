namespace GameClient.Features.MainMenu.UI.Dto;

public readonly struct AccountCreationAttempt
{
    public string Username { get; init; }
    public string Email { get; init; }
    public string Password { get; init; }
}
