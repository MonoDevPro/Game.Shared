namespace GameClient.Features.MainMenu.UI.Dto;

public readonly struct AccountLoginAttempt
{
    public string Username { get; init; }
    public string Password { get; init; }
}
