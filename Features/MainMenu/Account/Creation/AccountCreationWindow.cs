using System;
using GameClient.Core.UI;
using GameClient.Features.MainMenu.UI.Contracts;
using GameClient.Features.MainMenu.UI.Dto;
using Godot;

namespace GameClient.Features.MainMenu.Account.Creation;

public partial class AccountCreationWindow : BaseWindow, IAccountCreationView
{
    public event Action<AccountCreationAttempt> OnCreateAttempted; // username, email, password
    public event Action OnNavigateBackToLogin;

    // Interface events mapped to existing events
    public event Action<AccountCreationAttempt> CreateAttempted { add => OnCreateAttempted += value; remove => OnCreateAttempted -= value; }
    public event Action NavigateBackToLogin { add => OnNavigateBackToLogin += value; remove => OnNavigateBackToLogin -= value; }

    [Export] private LineEdit _usernameInput;
    [Export] private LineEdit _emailInput;
    [Export] private LineEdit _passwordInput;
    [Export] private LineEdit _confirmPasswordInput;
    [Export] private Button _createButton;
    [Export] private Label _errorLabel;
    [Export] private Label _busyLabel;

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
        SetBusy(false);
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
            Username = _usernameInput.Text,
            Email = _emailInput.Text,
            Password = _passwordInput.Text
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

    public void SetBusy(bool isBusy)
    {
        _createButton.Disabled = isBusy;
        _usernameInput.Editable = !isBusy;
        _emailInput.Editable = !isBusy;
        _passwordInput.Editable = !isBusy;
        _confirmPasswordInput.Editable = !isBusy;
        if (_busyLabel != null)
        {
            _busyLabel.Visible = isBusy;
            if (isBusy) _busyLabel.Text = "Aguarde...";
        }
    }

    public override void _ExitTree()
    {
        // Desconecta os sinais para evitar vazamentos de memória
        _createButton.Pressed -= OnCreateButtonPressed;
        this.CloseRequested -= OnBackButtonPressed;
        base._ExitTree();
    }
}