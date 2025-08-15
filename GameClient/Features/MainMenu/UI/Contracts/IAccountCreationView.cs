using System;
using GameClient.Features.MainMenu.UI.Dto;

namespace GameClient.Features.MainMenu.UI.Contracts;

public interface IAccountCreationView
{
    event Action<AccountCreationAttempt> CreateAttempted;
    event Action NavigateBackToLogin;

    void ShowError(string message);
    void ShowWindow();
    void HideWindow();
    void SetBusy(bool isBusy);
}
