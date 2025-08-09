using System;
using GameClient.Presentation.UI.Resources;
using Godot;
using Shared.Core.Enums;
using Shared.Infrastructure.Network.Data.Join;
using Shared.Infrastructure.Network.Transport;

namespace GameClient.Presentation.UI;

public partial class CreateCharacterScript : Window
{
    [Export] private NodePath _txtNamePath;
    private TextBox _txtName;
    
    [Export] private NodePath _optGenderPath;
    private OptionButton _optGender;
    
    [Export] private NodePath _optVocationPath;
    private OptionButton _optVocation;
    
    [Export] private NodePath _btnCreatePath;
    private Button _btnCreate;

    private NetworkSender Sender => ClientBootstrap.Instance.ClientNetwork.Sender;
    
    public override void _Ready()
    {
        // Initialize the TextBox for character name input
        _txtName = GetNode<TextBox>(_txtNamePath);
        _txtName.Type = TextBox.TextBoxType.CharacterName;
        _txtName.PlaceholderText = "Enter your character name";
        
        // Initialize the OptionButtons
        _optGender = GetNode<OptionButton>(_optGenderPath);
        _optVocation = GetNode<OptionButton>(_optVocationPath);
        
        // Limpa itens existentes para garantir que não haja duplicatas
        _optGender.Clear();
        _optVocation.Clear();

        // Popula o OptionButton de Gênero
        foreach (var gender in Enum.GetValues<GenderEnum>())
        {
            // Pula a opção "None" para que não seja selecionável pelo jogador
            if (gender == GenderEnum.None)
                continue;
            
            _optGender.AddItem(gender.ToString(), (int)gender);
        }

        // Popula o OptionButton de Vocação
        foreach (var vocation in Enum.GetValues<VocationEnum>())
        {
            // Pula a opção "None"
            if (vocation == VocationEnum.None)
                continue;
            
            _optVocation.AddItem(vocation.ToString(), (int)vocation);
        }

        _btnCreate = GetNode<Button>(_btnCreatePath);
        
        _btnCreate.Pressed += OnCreateButtonPressed;
        
        base._Ready();
    }
    
    private void OnCreateButtonPressed()
    {
        var selectedGenderId = _optGender.GetSelectedId();
        var selectedVocationId = _optVocation.GetSelectedId();

        var chosenGender = (GenderEnum)selectedGenderId;
        var chosenVocation = (VocationEnum)selectedVocationId;

        // Agora você tem os enums corretos para enviar ao servidor!
        GD.Print($"Gênero escolhido: {chosenGender}, Vocação: {chosenVocation}");
        
        var packet = new JoinRequest
        {
            Name = _txtName.Text, 
            Gender = chosenGender, 
            Vocation = chosenVocation
        };
        
        Sender.EnqueueReliableSend(0, ref packet);
        
        // Optionally, you can hide the create character window after sending the request
        Hide();
    }
    
    public override void _ExitTree()
    {
        base._ExitTree();
        
        // Disconnect the button signal to avoid memory leaks
        if (_btnCreate != null)
            _btnCreate.Pressed -= OnCreateButtonPressed;
    }
}