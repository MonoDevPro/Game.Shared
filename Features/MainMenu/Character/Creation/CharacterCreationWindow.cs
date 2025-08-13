using System;
using GameClient.Core.UI;
using Godot;
using Shared.Core.Common.Enums;
using Shared.Features.MainMenu.Character;

namespace GameClient.Features.MainMenu.Character.Creation;

public partial class CharacterCreationWindow : BaseWindow
{
    public event Action<CharacterCreationAttempt> OnCreateAttempted;

    public event Action OnNavigateBackToCharacterList;

    [Export]private LineEdit _nameInput;
    [Export]private OptionButton _vocationOptions;
    [Export]private OptionButton _genderOptions;
    [Export]private Button _createButton;
    [Export]private Label _errorLabel;

    public override void _Ready()
    {
        base._Ready();
        
        // Popula o OptionButton de Gênero
        foreach (var gender in Enum.GetValues<GenderEnum>())
        {
            // Pula a opção "None" para que não seja selecionável pelo jogador
            if (gender == GenderEnum.None)
                continue;
            
            _genderOptions.AddItem(gender.ToString(), (int)gender);
        }

        // Popula o OptionButton de Vocação
        foreach (var vocation in Enum.GetValues<VocationEnum>())
        {
            // Pula a opção "None"
            if (vocation == VocationEnum.None)
                continue;
            
            _vocationOptions.AddItem(vocation.ToString(), (int)vocation);
        }

        _createButton.Pressed += OnCreateButtonPressed;
    }

    protected override void OnCloseRequested()
    {
        OnNavigateBackToCharacterList?.Invoke();
        base.OnCloseRequested();
    }

    protected override void OnWindowShown()
    {
        _nameInput.Clear();
        _nameInput.GrabFocus();
        _errorLabel.Hide();
    }
    
    public void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.Show();
    }

    private void OnCreateButtonPressed()
    {
        _errorLabel.Hide();
        
        var selectedName = _nameInput.Text.Trim();
        var selectedGenderId = _genderOptions.GetSelectedId();
        var selectedVocationId = _vocationOptions.GetSelectedId();

        if (string.IsNullOrEmpty(selectedName) || 
            selectedName.Length < CharacterConstants.MinCharacterNameLength || 
            selectedName.Length > CharacterConstants.MaxCharacterNameLength)
        {
            ShowError($"O nome deve ter entre {CharacterConstants.MinCharacterNameLength} e {CharacterConstants.MaxCharacterNameLength} caracteres.");
            return;
        }
        
        var chosenGender = (GenderEnum)selectedGenderId;
        var chosenVocation = (VocationEnum)selectedVocationId;

        GD.Print($"Nome escolhido: {selectedName}, Gênero escolhido: {chosenGender}, Vocação: {chosenVocation}");
        
        var data = new CharacterCreationAttempt
        {
            Name = selectedName, Vocation = chosenVocation, Gender = chosenGender
        };
        OnCreateAttempted?.Invoke(data);
    }
    
    public override void _ExitTree()
    {
        // Desconecta o sinal do botão de criação para evitar vazamentos de memória
        _createButton.Pressed -= OnCreateButtonPressed;
        base._ExitTree();
    }
}