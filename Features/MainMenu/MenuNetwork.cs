using System;
using System.Collections.Generic;
using GameClient.Core.Services;
using GameClient.Features.MainMenu.UI.Contracts;
using GameClient.Features.MainMenu.UI.Dto;
using LiteNetLib;
using Shared.Core.Network;
using Shared.Features.Game.Character.Packets.Enter;
using Shared.Features.MainMenu;
using Shared.Features.MainMenu.Account.AccountCreation;
using Shared.Features.MainMenu.Account.AccountLogin;
using Shared.Features.MainMenu.Account.AccountLogout;
using Shared.Features.MainMenu.Character;
using Shared.Features.MainMenu.Character.CharacterCreation;
using Shared.Features.MainMenu.Character.CharacterList;
using Shared.Features.MainMenu.Character.CharacterSelection;

namespace GameClient.Features.MainMenu;

public class MenuNetwork : IDisposable
{
    private readonly NetworkManager _networkManager;
    private readonly SelectedCharacterService _selectedCharacter;
    private readonly List<IDisposable> _networkSubscriptions = [];

    public void Poll() => _networkManager.PollEvents();

    #region Definições de Resultados de Fluxo
    // Usando 'readonly struct' e 'init' para garantir imutabilidade após a criação.
    public readonly struct AccountLoginFlowResult { public bool Success { get; init; } public string Message { get; init; } public CharacterDto[] Characters { get; init; } }
    public readonly struct AccountCreationFlowResult { public bool Success { get; init; } public string Message { get; init; } }
    public readonly struct CharacterCreationFlowResult { public bool Success { get; init; } public string Message { get; init; } public CharacterDto Character { get; init; } }
    public readonly struct EnterGameFlowResult { public bool Success { get; init; } public string Message { get; init; } }
    #endregion

    #region Eventos de Conclusão de Fluxo (API Pública)
    // Agora temos UM evento de conclusão para cada fluxo, tornando a API mais limpa.
    public event Action<AccountLoginFlowResult> OnLoginFlowCompleted;
    public event Action<AccountCreationFlowResult> OnCreateAccountFlowCompleted;
    public event Action<CharacterCreationFlowResult> OnCreateCharacterFlowCompleted;
    public event Action<EnterGameFlowResult> OnEnterGameFlowCompleted;
    #endregion

    // Referências às views (injetadas via construtor)
    private readonly ILoginView _loginView;
    private readonly IAccountCreationView _createAccountView;
    private readonly ICharacterCreationView _characterCreationView;
    private readonly ICharacterSelectionView _characterSelectionView;

    public MenuNetwork(NetworkManager networkManager, ILoginView loginView, IAccountCreationView createAccountView,
        ICharacterCreationView characterCreationView, ICharacterSelectionView characterSelectionView, SelectedCharacterService selectedCharacter)
    {
        _networkManager = networkManager;
        _selectedCharacter = selectedCharacter;
        _loginView = loginView;
        _createAccountView = createAccountView;
        _characterCreationView = characterCreationView;
        _characterSelectionView = characterSelectionView;

        // Inscreve-se nos eventos de AÇÃO das janelas
        _loginView.LoginAttempted += HandleAccountLoginAttempt;
        _createAccountView.CreateAttempted += HandleAccountCreationAttempt;
        _characterCreationView.CreateAttempted += HandleCharacterCreationAttempt;
        _characterSelectionView.CharacterSelected += HandleCharacterSelectionAttempt; // Isto agora é a tentativa de entrar no jogo
        _characterSelectionView.Logout += HandleAccountLogoutAttempt;

        // Inscreve-se nas RESPOSTAS da Rede
        _networkSubscriptions.Add(_networkManager.Receiver.RegisterMessageHandler<AccountLoginResponse>(OnAccountLoginResponse));
        _networkSubscriptions.Add(_networkManager.Receiver.RegisterMessageHandler<CharacterListResponse>(OnCharacterListResponse));
        _networkSubscriptions.Add(_networkManager.Receiver.RegisterMessageHandler<AccountCreationResponse>(OnAccountCreationResponse));
        _networkSubscriptions.Add(_networkManager.Receiver.RegisterMessageHandler<CharacterCreationResponse>(OnCharacterCreationResponse));
        _networkSubscriptions.Add(_networkManager.Receiver.RegisterMessageHandler<CharacterSelectionResponse>(OnEnterGameResponse));
    }

    // --- Handlers de Ações da UI ---

    private void HandleAccountLoginAttempt(AccountLoginAttempt attempt)
    {
        _loginView.SetBusy(true);
        var packet = new AccountLoginRequest { Username = attempt.Username, Password = attempt.Password };
        _networkManager.Sender.SendNow(ref packet, 0, DeliveryMethod.ReliableOrdered);
    }

    private void HandleAccountCreationAttempt(AccountCreationAttempt attempt)
    {
        _createAccountView.SetBusy(true);
        var packet = new AccountCreationRequest { Username = attempt.Username, Email = attempt.Email, Password = attempt.Password };
        _networkManager.Sender.SendNow(ref packet, 0, DeliveryMethod.ReliableOrdered);
    }

    private void HandleCharacterCreationAttempt(CharacterCreationAttempt attempt)
    {
        _characterCreationView.SetBusy(true);
        var packet = new CharacterCreationRequest { Name = attempt.Name, Vocation = attempt.Vocation, Gender = attempt.Gender };
        _networkManager.Sender.SendNow(ref packet, 0, DeliveryMethod.ReliableOrdered);
    }

    private void HandleCharacterSelectionAttempt(CharacterSelectionAttempt attempt)
    {
        _characterSelectionView.SetBusy(true);
        // Primeiro, informe o servidor qual personagem foi selecionado (fluxo de menu)
        var selection = new CharacterSelectionRequest { CharacterId = attempt.CharacterId };
        _networkManager.Sender.SendNow(ref selection, 0, DeliveryMethod.ReliableOrdered);
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
        _loginView.SetBusy(false);
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
        _createAccountView.SetBusy(false);
        // Monta e dispara o resultado único.
        var result = new AccountCreationFlowResult { Success = packet.Success, Message = packet.Message };
        OnCreateAccountFlowCompleted?.Invoke(result);
    }

    private void OnCharacterCreationResponse(CharacterCreationResponse packet, NetPeer peer)
    {
        _characterCreationView.SetBusy(false);
        // Monta e dispara o resultado único.
        var result = new CharacterCreationFlowResult { Success = packet.Success, Message = packet.Message, Character = packet.Character };
        OnCreateCharacterFlowCompleted?.Invoke(result);
    }

    private void OnEnterGameResponse(CharacterSelectionResponse packet, NetPeer peer)
    {
        _characterSelectionView.SetBusy(false);
        // Se a seleção de personagem foi aceita, envia o EnterGameRequest para spawnar no mundo
        if (packet.Success)
        {
            _selectedCharacter.SelectedCharacterId = packet.Character.CharacterId;
            var enter = new EnterGameRequest { CharacterId = packet.Character.CharacterId };
            _networkManager.Sender.SendNow(ref enter, 0, DeliveryMethod.ReliableOrdered);
        }

        var result = new EnterGameFlowResult { Success = packet.Success, Message = packet.Message };
        OnEnterGameFlowCompleted?.Invoke(result);
    }

    public void Dispose()
    {
        foreach (var subscription in _networkSubscriptions)
            subscription.Dispose();
        _networkSubscriptions.Clear();

        // Cancela a inscrição dos eventos das views para segurança.
        _loginView.LoginAttempted -= HandleAccountLoginAttempt;
        _createAccountView.CreateAttempted -= HandleAccountCreationAttempt;
        _characterCreationView.CreateAttempted -= HandleCharacterCreationAttempt;
        _characterSelectionView.CharacterSelected -= HandleCharacterSelectionAttempt;
        _characterSelectionView.Logout -= HandleAccountLogoutAttempt;
    }
}