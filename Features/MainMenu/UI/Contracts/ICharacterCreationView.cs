using System;
using GameClient.Features.MainMenu.UI.Dto;

namespace GameClient.Features.MainMenu.UI.Contracts;

public interface ICharacterCreationView
{
    event Action<CharacterCreationAttempt> CreateAttempted;
    event Action NavigateBackToCharacterList;

    void ShowError(string message);
    void ShowWindow();
    void HideWindow();
    void SetBusy(bool isBusy);
}
