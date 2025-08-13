// Local: Features/Authentication/LoginWindow.cs

using System;
using GameClient.Core.UI;
using GameClient.Features.MainMenu.UI.Contracts;
using GameClient.Features.MainMenu.UI.Dto;
using Godot;

namespace GameClient.Features.MainMenu.Account.Login;

public partial class AccountLoginWindow : BaseWindow, ILoginView
{
    // Eventos que esta janela pode disparar
    public event Action<AccountLoginAttempt> OnLoginAttempted;
    public event Action OnNavigateToCreateAccount;

    // Interface events mapped to existing events
    public event Action<AccountLoginAttempt> LoginAttempted { add => OnLoginAttempted += value; remove => OnLoginAttempted -= value; }
    public event Action NavigateToCreateAccount { add => OnNavigateToCreateAccount += value; remove => OnNavigateToCreateAccount -= value; }

    // Referências aos nós da cena
    [Export] private LineEdit _usernameInput;
    [Export] private LineEdit _passwordInput;
    [Export] private Button _loginButton;
    [Export] private Button _createAccountButton;
    [Export] private Label _errorLabel;
    [Export] private Label _busyLabel;

    public override void _Ready()
    {
        base._Ready(); // Importante chamar o método da classe base!

        // Conectar os sinais dos botões
        _loginButton.Pressed += OnLoginButtonPressed;
        _createAccountButton.Pressed += OnCreateAccountButtonPressed;
    }

    protected override void OnWindowShown()
    {
        _usernameInput.GrabFocus();
        _errorLabel.Hide(); // Esconde a mensagem de erro ao abrir
        SetBusy(false);
    }

    private void OnLoginButtonPressed()
    {
        if (string.IsNullOrWhiteSpace(_usernameInput.Text) || string.IsNullOrWhiteSpace(_passwordInput.Text))
        {
            ShowError("Por favor, preencha todos os campos.");
            return;
        }

        _errorLabel.Hide();
        OnLoginAttempted?.Invoke(new AccountLoginAttempt { Username = _usernameInput.Text, Password = _passwordInput.Text });
    }

    private void OnCreateAccountButtonPressed()
    {
        OnNavigateToCreateAccount?.Invoke();
    }

    // Método público que o gerenciador pode chamar para mostrar um erro
    public void ShowError(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.Show();
    }

    public void SetBusy(bool isBusy)
    {
        _loginButton.Disabled = isBusy;
        _createAccountButton.Disabled = isBusy;
        _usernameInput.Editable = !isBusy;
        _passwordInput.Editable = !isBusy;
        if (_busyLabel != null)
        {
            _busyLabel.Visible = isBusy;
            if (isBusy) _busyLabel.Text = "Aguarde...";
        }
    }

    public override void _ExitTree()
    {
        // Desconectar os sinais para evitar vazamentos de memória
        _loginButton.Pressed -= OnLoginButtonPressed;
        _createAccountButton.Pressed -= OnCreateAccountButtonPressed;

        base._ExitTree();
    }
}