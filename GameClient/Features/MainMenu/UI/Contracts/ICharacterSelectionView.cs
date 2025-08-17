using System;
using GameClient.Features.MainMenu.UI.Dto;
using Shared.Network.Packets.MainMenu.Character;

namespace GameClient.Features.MainMenu.UI.Contracts;

public interface ICharacterSelectionView
{
    event Action<CharacterSelectionAttempt> CharacterSelected;
    event Action NavigateToCreateCharacter;
    event Action Logout;

    void PopulateCharacterList(CharacterData[] characters);
    void AddCharacterEntry(CharacterData character);
    void ShowError(string message);
    void ShowWindow();
    void HideWindow();
    void SetBusy(bool isBusy);
}
