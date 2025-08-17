using System;
using Game.Core.Entities.Character;
using GameClient.Core.UI;
using GameClient.Features.MainMenu.UI.Contracts;
using GameClient.Features.MainMenu.UI.Dto;
using Godot;
using Shared.Network.Packets.MainMenu.Character;

namespace GameClient.Features.MainMenu.Character.Selection;

public partial class CharacterSelectionWindow : BaseWindow, ICharacterSelectionView
{
    public event Action<CharacterSelectionAttempt> OnCharacterSelected; // Envia o ID do personagem

    public event Action OnNavigateToCreateCharacter;

    public event Action OnLogout;

    // Interface events mapped to existing events
    public event Action<CharacterSelectionAttempt> CharacterSelected { add => OnCharacterSelected += value; remove => OnCharacterSelected -= value; }
    public event Action NavigateToCreateCharacter { add => OnNavigateToCreateCharacter += value; remove => OnNavigateToCreateCharacter -= value; }
    public event Action Logout { add => OnLogout += value; remove => OnLogout -= value; }

    [Export] private ItemList _characterList;
    [Export] private Button _selectButton;
    [Export] private Button _createCharacterButton;
    [Export] private Label _errorLabel;
    [Export] private Label _busyLabel;

    public override void _Ready()
    {
        base._Ready();

        // Conecta o sinal de Seleção do personagem
        _selectButton.Pressed += OnSelectButtonPressed;
        // Conecta o sinal de seleção do personagem a partir da lista
        _characterList.ItemActivated += OnSelectDoubleClicked;
        // Conecta o sinal de criação de personagem
        _createCharacterButton.Pressed += OnCreateCharacterPressed;
        // Logou se não houver personagens selecionados e a janela for fechada
        this.CloseRequested += OnLogoutPressed;
    }

    protected override void OnWindowShown()
    {
        _characterList.GrabFocus();
        _errorLabel.Hide(); // Esconde a mensagem de erro ao abrir
        SetBusy(false);
    }

    private void OnSelectButtonPressed()
    {
        var items = _characterList.GetSelectedItems();
        if (items.Length == 0)
        {
            ShowError("Por favor, selecione um personagem.");
            return;
        }
        AttemptToSelectCharacter(items[0]);
    }
    private void OnSelectDoubleClicked(long index)
    {
        AttemptToSelectCharacter((int)index);
    }
    private void OnCreateCharacterPressed()
    {
        if (_characterList.ItemCount >= CharacterConstants.MaxCharacterCount)
        {
            ShowError("Você já atingiu o número máximo de personagens permitidos.");
            return;
        }
        // Dispara o evento para navegar para a criação de personagem
        OnNavigateToCreateCharacter?.Invoke();
    }
    private void OnLogoutPressed()
    {
        if (_characterList.ItemCount == 0 || _characterList.GetSelectedItems().Length == 0)
            OnLogout?.Invoke();
    }

    private void AttemptToSelectCharacter(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= _characterList.ItemCount)
        {
            ShowError("Por favor, selecione um personagem.");
            return;
        }

        var metadata = _characterList.GetItemMetadata(itemIndex);
        if (!metadata.VariantType.HasFlag(Variant.Type.Int))
        {
            ShowError("ID de personagem inválido.");
            return;
        }

        var characterId = metadata.AsInt32();
        OnCharacterSelected?.Invoke(new CharacterSelectionAttempt { CharacterId = characterId });
    }

    public void AddCharacterEntry(CharacterData character)
    {
        // Configura o texto da entrada
        string entry = $"{character.Name} - " +
                       $"Voc: {character.Vocation.ToString()} " +
                       $"Sex: {character.Gender.ToString()}";

        // Adiciona a entrada à lista de personagens
        var listIndex = _characterList.AddItem(entry);

        // Armazena o ID do personagem como metadado (opcional, mas útil)
        _characterList.SetItemMetadata(listIndex, character.CharacterId);

        // Seleciona o item adicionado
        _characterList.Select(listIndex);
    }

    public void PopulateCharacterList(CharacterData[] characters)
    {
        // Limpa a lista antiga
        _characterList.Clear();

        if (characters.Length == 0)
        {
            // Mostra uma mensagem "Nenhum personagem encontrado"
            ShowError("Nenhum personagem encontrado. Crie um novo personagem para começar.");
            return;
        }

        // Adiciona as novas entradas
        foreach (var character in characters)
            AddCharacterEntry(character);

        // Seleciona o primeiro item da lista, se a lista não estiver vazia
        if (_characterList.ItemCount > 0 && _characterList.GetItemMetadata(0).VariantType.HasFlag(Variant.Type.Int))
        {
            _characterList.Select(0);
        }
    }

    public void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.Show();
    }

    public void SetBusy(bool isBusy)
    {
        _selectButton.Disabled = isBusy;
        _createCharacterButton.Disabled = isBusy;

        foreach (var item in _characterList.GetSelectedItems())
        {
            _characterList.SetItemDisabled(item, isBusy);
        }

        if (_busyLabel != null)
        {
            _busyLabel.Visible = isBusy;
            if (isBusy) _busyLabel.Text = "Aguarde...";
        }
    }

    public override void _ExitTree()
    {
        // Desconecta os sinais para evitar vazamentos de memória
        _selectButton.Pressed -= OnSelectButtonPressed;
        _characterList.ItemActivated -= OnSelectDoubleClicked;
        _createCharacterButton.Pressed -= OnCreateCharacterPressed;
        this.CloseRequested -= OnLogoutPressed;

        base._ExitTree();
    }
}
