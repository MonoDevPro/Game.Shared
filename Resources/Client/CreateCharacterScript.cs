using Game.Shared.Scripts.Shared.Network;
using Game.Shared.Scripts.Shared.Network.Data.Join;
using Game.Shared.Scripts.Shared.Network.Transport;
using Godot;

namespace Game.Shared.Resources.Client;

public partial class CreateCharacterScript : Window
{
    [Export] private NodePath _networkManagerPath;
    private NetworkSender _sender;
    
    [Export] private NodePath _txtNamePath;
    private TextBox _txtName;
    
    [Export] private NodePath _btnCreatePath;
    private Button _btnCreate;
    
    public override void _Ready()
    {
        base._Ready();
        
        // Initialize the NetworkSender
        _sender = GetNode<NetworkManager>(_networkManagerPath).Sender;
        
        // Initialize the TextBox for character name input
        _txtName = GetNode<TextBox>(_txtNamePath);
        _txtName.Type = TextBox.TextBoxType.CharacterName;
        
        _btnCreate = GetNode<Button>(_btnCreatePath);
        _btnCreate.Pressed += OnCreateButtonPressed;
    }
    
    private void OnCreateButtonPressed()
    {
        
        // Proceed with character creation logic
        GD.Print($"Creating character with name: {_txtName.Text}");
        
        var packet = new JoinRequest { Name = _txtName.Text };
        
        _sender.Send(ref packet);
        
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
