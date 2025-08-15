using System;
using GameClient.Features.MainMenu.UI.Dto;

namespace GameClient.Features.MainMenu.UI.Contracts;

public interface ILoginView
{
    event Action<AccountLoginAttempt> LoginAttempted;
    event Action NavigateToCreateAccount;

    void ShowError(string message);
    void ShowWindow();
    void HideWindow();
    void SetBusy(bool isBusy);
}
