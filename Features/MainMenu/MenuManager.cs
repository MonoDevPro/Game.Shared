// Local: Features/MainMenu/MenuManager.cs
using GameClient.Core.Services;
using GameClient.Features.MainMenu.Account;
using GameClient.Features.MainMenu.Account.Creation;
using GameClient.Features.MainMenu.Account.Login;
using GameClient.Features.MainMenu.Character;
using GameClient.Features.MainMenu.Character.Creation;
using GameClient.Features.MainMenu.Character.Selection;
// Usings das suas janelas...
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Network;

namespace GameClient.Features.MainMenu;

public partial class MenuManager : Control
{
    public override void _Process(double delta)
    {
        // Chamamos o Poll do MenuNetwork para processar eventos de rede no main menu.
        _menuNetwork.Poll();
    }

    #region Referências às Janelas e Serviços
    // As referências para as janelas continuam aqui
    [Export] private AccountLoginWindow _loginWindow;
    [Export] private AccountCreationWindow _createAccountWindow;
    [Export] private CharacterSelectionWindow _characterSelectionWindow;
    [Export] private CharacterCreationWindow _createCharacterWindow;

    // A única dependência externa agora é o nosso novo helper de rede
    private MenuNetwork _menuNetwork;
    #endregion

    public override void _Ready()
    {
        // 1. Resolve o NetworkManager via DI para injetar no MenuNetwork
        var networkManager = GameServiceProvider.Instance.Services.GetRequiredService<NetworkManager>();

        // 2. Criamos e configuramos nosso 'MenuNetwork', que cuidará de toda a comunicação.
        // Passamos todas as dependências que ele precisa para funcionar.
        _menuNetwork = new MenuNetwork(networkManager, _loginWindow, _createAccountWindow, _createCharacterWindow, _characterSelectionWindow);

        // 3. O MenuManager agora se inscreve nos eventos de ALTO NÍVEL do MenuNetwork
        _menuNetwork.OnLoginFlowCompleted += HandleLoginFlowCompleted;
        _menuNetwork.OnCreateAccountFlowCompleted += HandleCreateAccountFlowCompleted;
        _menuNetwork.OnCreateCharacterFlowCompleted += HandleCreateCharacterFlowCompleted;
        _menuNetwork.OnEnterGameFlowCompleted += HandleEnterGameFlowCompleted;

        // 4. O MenuManager continua responsável pela NAVEGAÇÃO pura entre janelas
        _loginWindow.OnNavigateToCreateAccount += HandleNavigateToCreateAccount;
        _createAccountWindow.OnNavigateBackToLogin += HandleNavigateBackToLogin;
        _characterSelectionWindow.OnNavigateToCreateCharacter += HandleNavigateToCreateCharacter;
        _createCharacterWindow.OnNavigateBackToCharacterList += HandleNavigateBackToCharList;
        _characterSelectionWindow.OnLogout += HandleLogout;

        // Inicia o fluxo mostrando a janela de login
        _loginWindow.ShowWindow();
    }

    // --- Handlers que reagem aos RESULTADOS do MenuNetwork ---

    private void HandleLoginFlowCompleted(MenuNetwork.AccountLoginFlowResult result)
    {
        // Lógica de UI baseada no resultado do fluxo completo de login
        if (result.Success)
        {
            _loginWindow.HideWindow();
            _characterSelectionWindow.PopulateCharacterList(result.Characters);
            _characterSelectionWindow.ShowWindow();
        }
        else
        {
            _loginWindow.ShowError(result.Message);
        }
    }

    private void HandleCreateAccountFlowCompleted(MenuNetwork.AccountCreationFlowResult result)
    {
        if (result.Success)
        {
            // Mostra uma notificação de sucesso e navega de volta para o login
            // (Você poderia criar uma janela de Popup genérica para isso)
            GD.Print("Conta criada com sucesso! Por favor, faça o login.");
            _createAccountWindow.HideWindow();
            _loginWindow.ShowWindow();
        }
        else
        {
            _createAccountWindow.ShowError(result.Message);
        }
    }

    private void HandleCreateCharacterFlowCompleted(MenuNetwork.CharacterCreationFlowResult result)
    {
        if (result.Success)
        {
            // Personagem foi criado, adiciona ele na lista e volta para a seleção
            _characterSelectionWindow.AddCharacterEntry(result.Character);
            HandleNavigateBackToCharList();
        }
        else
        {
            // Mostra o erro na janela de criação de personagem
            _createCharacterWindow.ShowError(result.Message);
        }
    }

    private void HandleEnterGameFlowCompleted(MenuNetwork.EnterGameFlowResult result)
    {
        if (result.Success)
        {
            GD.Print("MenuManager: Autorizado a entrar no jogo! Trocando de cena...");
            // Ex: GetTree().ChangeSceneToFile("res://Scenes/World.tscn");
        }
        else
        {
            // Mostra um erro na janela de seleção de personagem
            _characterSelectionWindow.ShowError(result.Message);
        }
    }

    // --- Handlers que reagem à NAVEGAÇÃO da UI ---

    private void HandleNavigateToCreateAccount()
    {
        _loginWindow.HideWindow();
        _createAccountWindow.ShowWindow();
    }

    private void HandleNavigateBackToLogin()
    {
        _createAccountWindow.HideWindow();
        _loginWindow.ShowWindow();
    }

    private void HandleNavigateToCreateCharacter()
    {
        _characterSelectionWindow.HideWindow();
        _createCharacterWindow.ShowWindow();
    }

    private void HandleNavigateBackToCharList()
    {
        _createCharacterWindow.HideWindow();
        _characterSelectionWindow.ShowWindow();
    }

    private void HandleLogout()
    {
        // O logout agora é apenas uma navegação de UI
        GD.Print("MenuManager: Jogador deslogou, voltando para a tela de login.");
        _characterSelectionWindow.HideWindow();
        _loginWindow.ShowWindow();
    }

    public override void _ExitTree()
    {
        _menuNetwork.OnLoginFlowCompleted -= HandleLoginFlowCompleted;
        _menuNetwork.OnCreateAccountFlowCompleted -= HandleCreateAccountFlowCompleted;
        _menuNetwork.OnCreateCharacterFlowCompleted -= HandleCreateCharacterFlowCompleted;
        _menuNetwork.OnEnterGameFlowCompleted -= HandleEnterGameFlowCompleted;

        _loginWindow.OnNavigateToCreateAccount -= HandleNavigateToCreateAccount;
        _createAccountWindow.OnNavigateBackToLogin -= HandleNavigateBackToLogin;
        _characterSelectionWindow.OnNavigateToCreateCharacter -= HandleNavigateToCreateCharacter;
        _createCharacterWindow.OnNavigateBackToCharacterList -= HandleNavigateBackToCharList;
        _characterSelectionWindow.OnLogout -= HandleLogout;

        // A única responsabilidade de limpeza do MenuManager agora é descartar o MenuNetwork,
        // que por sua vez cuidará de limpar suas próprias inscrições de rede.
        _menuNetwork?.Dispose();

        base._ExitTree();
    }
}