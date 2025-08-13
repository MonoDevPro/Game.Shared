using System;
using GameClient.Features.MainMenu.UI.Dto;
using Shared.Features.MainMenu.Character;

namespace GameClient.Features.MainMenu.UI.Contracts;

public interface ICharacterSelectionView
{
    event Action<CharacterSelectionAttempt> CharacterSelected;
    event Action NavigateToCreateCharacter;
    event Action Logout;

    void PopulateCharacterList(CharacterDataModel[] characters);
    void AddCharacterEntry(CharacterDataModel character);
    void ShowError(string message);
    void ShowWindow();
    void HideWindow();
    void SetBusy(bool isBusy);
}
