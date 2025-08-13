using System;
using GameClient.Core.UI;
using Godot;

namespace GameClient.Features.MainMenu.Account.Creation;

public partial class AccountCreationWindow : BaseWindow
{
    public event Action<AccountCreationAttempt> OnCreateAttempted; // username, email, password
    public event Action OnNavigateBackToLogin;

    [Export]private LineEdit _usernameInput;
    [Export]private LineEdit _emailInput;
    [Export]private LineEdit _passwordInput;
    [Export]private LineEdit _confirmPasswordInput;
    [Export]private Button _createButton;
    [Export]private Label _errorLabel;

    public override void _Ready()
    {
        base._Ready();
        _createButton.Pressed += OnCreateButtonPressed;
        this.CloseRequested += OnBackButtonPressed;
    }

    protected override void OnWindowShown()
    {
        _usernameInput.GrabFocus();
        _errorLabel.Hide();
    }

    private void OnCreateButtonPressed()
    {
        _errorLabel.Hide();
        if (_passwordInput.Text != _confirmPasswordInput.Text)
        {
            ShowError("As senhas não coincidem!");
            return;
        }

        OnCreateAttempted?.Invoke(new AccountCreationAttempt
        {
            Username = _usernameInput.Text, Email = _emailInput.Text, Password = _passwordInput.Text
        });
    }

    private void OnBackButtonPressed()
    {
        OnNavigateBackToLogin?.Invoke();
    }
    
    public void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.Show();
    }

    public override void _ExitTree()
    {
        // Desconecta os sinais para evitar vazamentos de memória
        _createButton.Pressed -= OnCreateButtonPressed;
        this.CloseRequested -= OnBackButtonPressed;
        base._ExitTree();
    }
}