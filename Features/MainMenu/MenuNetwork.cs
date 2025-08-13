using System;
using System.Collections.Generic;
using GameClient.Features.MainMenu.Account.Creation;
using GameClient.Features.MainMenu.Account.Login;
using GameClient.Features.MainMenu.Character.Creation;
using GameClient.Features.MainMenu.Character.Selection;
using LiteNetLib;
using Shared.Core.Network;
using Shared.Features.MainMenu;
using Shared.Features.MainMenu.Account.AccountCreation;
using Shared.Features.MainMenu.Account.AccountLogin;
using Shared.Features.MainMenu.Account.AccountLogout;
using Shared.Features.MainMenu.Character;
using Shared.Features.MainMenu.Character.CharacterCreation;
using Shared.Features.MainMenu.Character.CharacterList;

namespace GameClient.Features.MainMenu;

public class MenuNetwork : IDisposable
{
    private readonly NetworkManager _networkManager;
    private readonly List<IDisposable> _networkSubscriptions = [];
    
    public void Poll() => _networkManager.PollEvents();
    
    #region Definições de Resultados de Fluxo
    // Usando 'readonly struct' e 'init' para garantir imutabilidade após a criação.
    public readonly struct AccountLoginFlowResult { public bool Success { get; init; } public string Message { get; init; } public CharacterDataModel[] Characters { get; init; } }
    public readonly struct AccountCreationFlowResult { public bool Success { get; init; } public string Message { get; init; } }
    public readonly struct CharacterCreationFlowResult { public bool Success { get; init; } public string Message { get; init; } public CharacterDataModel Character { get; init; } }
    public readonly struct EnterGameFlowResult { public bool Success { get; init; } public string Message { get; init; } }
    #endregion
    
    #region Eventos de Conclusão de Fluxo (API Pública)
    // Agora temos UM evento de conclusão para cada fluxo, tornando a API mais limpa.
    public event Action<AccountLoginFlowResult> OnLoginFlowCompleted;
    public event Action<AccountCreationFlowResult> OnCreateAccountFlowCompleted;
    public event Action<CharacterCreationFlowResult> OnCreateCharacterFlowCompleted;
    public event Action<EnterGameFlowResult> OnEnterGameFlowCompleted;
    #endregion
    
    // Referências às janelas (injetadas via construtor)
    private readonly AccountLoginWindow _loginWindow;
    private readonly AccountCreationWindow _createAccountWindow;
    private readonly CharacterCreationWindow _characterCreationWindow;
    private readonly CharacterSelectionWindow _characterSelectionWindow;

    public MenuNetwork(NetworkManager networkManager, AccountLoginWindow loginWindow, AccountCreationWindow createAccountWindow,
        CharacterCreationWindow characterCreationWindow, CharacterSelectionWindow characterSelectionWindow)
    {
        _networkManager = networkManager;
        _loginWindow = loginWindow;
        _createAccountWindow = createAccountWindow;
        _characterCreationWindow = characterCreationWindow;
        _characterSelectionWindow = characterSelectionWindow;

        // Inscreve-se nos eventos de AÇÃO das janelas
        _loginWindow.OnLoginAttempted += HandleAccountLoginAttempt;
        _createAccountWindow.OnCreateAttempted += HandleAccountCreationAttempt;
        _characterCreationWindow.OnCreateAttempted += HandleCharacterCreationAttempt;
        _characterSelectionWindow.OnCharacterSelected += HandleCharacterSelectionAttempt; // Isto agora é a tentativa de entrar no jogo
        _characterSelectionWindow.OnLogout += HandleAccountLogoutAttempt;

        // Inscreve-se nas RESPOSTAS da Rede
        _networkSubscriptions.Add(_networkManager.Receiver.RegisterMessageHandler<AccountLoginResponse>(OnAccountLoginResponse));
        _networkSubscriptions.Add(_networkManager.Receiver.RegisterMessageHandler<CharacterListResponse>(OnCharacterListResponse));
        _networkSubscriptions.Add(_networkManager.Receiver.RegisterMessageHandler<AccountCreationResponse>(OnAccountCreationResponse));
        _networkSubscriptions.Add(_networkManager.Receiver.RegisterMessageHandler<CharacterCreationResponse>(OnCharacterCreationResponse));
        _networkSubscriptions.Add(_networkManager.Receiver.RegisterMessageHandler<EnterGameResponse>(OnEnterGameResponse));
    }

    // --- Handlers de Ações da UI ---

    private void HandleAccountLoginAttempt(AccountLoginAttempt attempt)
    {
        var packet = new AccountLoginRequest { Username = attempt.Username, Password = attempt.Password };
        _networkManager.Sender.SendNow(ref packet, 0, DeliveryMethod.ReliableOrdered);
    }

    private void HandleAccountCreationAttempt(AccountCreationAttempt attempt)
    {
        var packet = new AccountCreationRequest { Username = attempt.Username, Email = attempt.Email, Password = attempt.Password };
        _networkManager.Sender.SendNow(ref packet, 0, DeliveryMethod.ReliableOrdered);
    }

    private void HandleCharacterCreationAttempt(CharacterCreationAttempt attempt)
    {
        var packet = new CharacterCreationRequest { Name = attempt.Name, Vocation = attempt.Vocation, Gender = attempt.Gender };
        _networkManager.Sender.SendNow(ref packet, 0, DeliveryMethod.ReliableOrdered);
    }

    private void HandleCharacterSelectionAttempt(CharacterSelectionAttempt attempt)
    {
        // A seleção de um personagem é a intenção de entrar no jogo com ele.
        var packet = new EnterGameRequest { CharacterId = attempt.CharacterId };
        _networkManager.Sender.SendNow(ref packet, 0, DeliveryMethod.ReliableOrdered);
    }

    private void HandleAccountLogoutAttempt()
    {
        var packet = new AccountLogoutRequest();
        _networkManager.Sender.SendNow(ref packet, 0, DeliveryMethod.ReliableOrdered);
        // Poderíamos também desconectar o cliente aqui ou esperar uma resposta do servidor.
    }

    // --- Handlers de Respostas da Rede ---

    private void OnAccountLoginResponse(AccountLoginResponse packet, NetPeer peer)
    {
        if (packet.Success)
        {
            // O login em si foi bem-sucedido, agora precisamos da lista de personagens.
            var characterListRequest = new CharacterListRequest();
            _networkManager.Sender.SendNow(ref characterListRequest, 0, DeliveryMethod.ReliableOrdered);
        }
        else
        {
            // Se o login falhou, o fluxo já acabou aqui.
            var result = new AccountLoginFlowResult { Success = false, Message = packet.Message };
            OnLoginFlowCompleted?.Invoke(result);
        }
    }

    private void OnCharacterListResponse(CharacterListResponse packet, NetPeer peer)
    {
        // Esta é a conclusão bem-sucedida do fluxo de login.
        var result = new AccountLoginFlowResult { Success = true, Message = "Login successful.", Characters = packet.Characters };
        OnLoginFlowCompleted?.Invoke(result);
    }

    private void OnAccountCreationResponse(AccountCreationResponse packet, NetPeer peer)
    {
        // Monta e dispara o resultado único.
        var result = new AccountCreationFlowResult { Success = packet.Success, Message = packet.Message };
        OnCreateAccountFlowCompleted?.Invoke(result);
    }

    private void OnCharacterCreationResponse(CharacterCreationResponse packet, NetPeer peer)
    {
        // Monta e dispara o resultado único.
        var result = new CharacterCreationFlowResult { Success = packet.Success, Message = packet.Message, Character = packet.Character };
        OnCreateCharacterFlowCompleted?.Invoke(result);
    }
    
    private void OnEnterGameResponse(EnterGameResponse packet, NetPeer peer)
    {
        // Monta e dispara o resultado único.
        var result = new EnterGameFlowResult { Success = packet.Success, Message = packet.Message };
        OnEnterGameFlowCompleted?.Invoke(result);
    }
    
    public void Dispose()
    {
        foreach (var subscription in _networkSubscriptions)
            subscription.Dispose();
        _networkSubscriptions.Clear();

        // Cancela a inscrição dos eventos das janelas para segurança.
        _loginWindow.OnLoginAttempted -= HandleAccountLoginAttempt;
        _createAccountWindow.OnCreateAttempted -= HandleAccountCreationAttempt;
        _characterCreationWindow.OnCreateAttempted -= HandleCharacterCreationAttempt;
        _characterSelectionWindow.OnCharacterSelected -= HandleCharacterSelectionAttempt;
        _characterSelectionWindow.OnLogout -= HandleAccountLogoutAttempt;
    }
}