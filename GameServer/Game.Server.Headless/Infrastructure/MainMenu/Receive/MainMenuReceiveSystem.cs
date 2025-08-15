using System.Text.RegularExpressions;
using Arch.Core;
using Arch.System;
using Game.Core.Entities.Character;
using Game.Core.Entities.Common.Rules;
using Game.Server.Headless.Infrastructure.Repositories;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Core.Network.Transport;
using Shared.Features.MainMenu.Account;
using Shared.Features.MainMenu.Account.AccountCreation;
using Shared.Features.MainMenu.Account.AccountLogin;
using Shared.Features.MainMenu.Account.AccountLogout;
using Shared.Features.MainMenu.Character;
using Shared.Features.MainMenu.Character.CharacterList;
using Shared.Features.MainMenu.Character.CharacterSelection;
using Shared.MainMenu.Character;
using Shared.MainMenu.Character.CharacterCreation;
using Shared.MainMenu.Character.CharacterList;

namespace Game.Server.Headless.Infrastructure.MainMenu.Receive;

public class MainMenuReceiveSystem : BaseSystem<World, float>
{
    private readonly ILogger<MainMenuReceiveSystem> _logger;
    private readonly NetworkReceiver _receiver;
    private readonly NetworkSender _sender;
    private readonly AccountRepository _accounts;
    private readonly CharacterRepository _characters;
    private readonly SessionService _sessions;
    private readonly List<IDisposable> _subs = [];

    public MainMenuReceiveSystem(
        World world,
        ILogger<MainMenuReceiveSystem> logger,
        NetworkReceiver receiver,
        NetworkSender sender,
        AccountRepository accounts,
        CharacterRepository characters,
        SessionService sessions) : base(world)
    {
        _logger = logger;
        _receiver = receiver;
        _sender = sender;
        _accounts = accounts;
        _characters = characters;
        _sessions = sessions;

        _subs.AddRange([
            receiver.RegisterMessageHandler<AccountCreationRequest>(OnAccountCreationRequest),
            receiver.RegisterMessageHandler<AccountLoginRequest>(OnAccountLoginRequest),
            receiver.RegisterMessageHandler<AccountLogoutRequest>(OnAccountLogoutRequest),
            receiver.RegisterMessageHandler<CharacterListRequest>(OnCharacterListRequest),
            receiver.RegisterMessageHandler<CharacterCreationRequest>(OnCharacterCreationRequest),
            receiver.RegisterMessageHandler<CharacterSelectionRequest>(OnCharacterSelectionRequest)
        ]);
    }

    public override void Update(in float t)
    {
        // No-op: event-driven
    }

    private void OnAccountCreationRequest(AccountCreationRequest packet, NetPeer peer)
    {
        _logger.LogInformation("AccountCreation from {Peer}", peer.Id);
        if (string.IsNullOrWhiteSpace(packet.Username) || string.IsNullOrWhiteSpace(packet.Email) || string.IsNullOrWhiteSpace(packet.Password))
        {
            var resp = new AccountCreationResponse { Success = false, Message = "Invalid fields" };
            _sender.EnqueueReliableSend(peer.Id, ref resp);
            return;
        }

        var username = packet.Username.Trim();
        var email = packet.Email.Trim();
        var password = packet.Password;
        
        if (!UsernameRule.IsValid(username))
        {
            var resp = new AccountCreationResponse { Success = false, Message = "Invalid username format" };
            _sender.EnqueueReliableSend(peer.Id, ref resp);
            return;
        }
        if (!EmailRule.IsValid(email))
        {
            var resp = new AccountCreationResponse { Success = false, Message = "Invalid email format" };
            _sender.EnqueueReliableSend(peer.Id, ref resp);
            return;
        }
        if (!PasswordRule.IsValid(password))
        {
            var resp = new AccountCreationResponse { Success = false, Message = "Invalid password format" };
            _sender.EnqueueReliableSend(peer.Id, ref resp);
            return;
        }

        if (_accounts.UsernameExists(username))
        {
            var resp = new AccountCreationResponse { Success = false, Message = "Username already exists" };
            _sender.EnqueueReliableSend(peer.Id, ref resp);
            return;
        }
        if (_accounts.EmailExists(email))
        {
            var resp = new AccountCreationResponse { Success = false, Message = "Email already exists" };
            _sender.EnqueueReliableSend(peer.Id, ref resp);
            return;
        }

        _accounts.Create(username, email, password);
        var ok = new AccountCreationResponse { Success = true, Message = "Account created" };
        _sender.EnqueueReliableSend(peer.Id, ref ok);
    }

    private void OnAccountLoginRequest(AccountLoginRequest packet, NetPeer peer)
    {
        _logger.LogInformation("AccountLogin from {Peer}", peer.Id);

        var username = packet.Username?.Trim() ?? string.Empty;
        var password = packet.Password ?? string.Empty;
        if (username.Length < AccountConstants.MinUsernameLength || username.Length > AccountConstants.MaxUsernameLength ||
            !Regex.IsMatch(username, AccountConstants.UsernameRegexPattern) ||
            password.Length < AccountConstants.MinPasswordLength || password.Length > AccountConstants.MaxPasswordLength)
        {
            var resp = new AccountLoginResponse { Success = false, Message = "Invalid username/password format" };
            _sender.EnqueueReliableSend(peer.Id, ref resp);
            return;
        }

        if (!_accounts.GetByUsername(username, out var acc))
        {
            var resp = new AccountLoginResponse { Success = false, Message = "Unknown user" };
            _sender.EnqueueReliableSend(peer.Id, ref resp);
            return;
        }
        if (acc!.Password != password)
        {
            var resp = new AccountLoginResponse { Success = false, Message = "Invalid password" };
            _sender.EnqueueReliableSend(peer.Id, ref resp);
            return;
        }

        _sessions.Bind(peer, acc.Id);
        var ok = new AccountLoginResponse { Success = true, Message = "Login ok" };
        _sender.EnqueueReliableSend(peer.Id, ref ok);
    }

    private void OnAccountLogoutRequest(AccountLogoutRequest packet, NetPeer peer)
    {
        _logger.LogInformation("AccountLogout from {Peer}", peer.Id);
        _sessions.Unbind(peer);
        var resp = new AccountLogoutResponse { Success = true, Message = "Logged out" };
        _sender.EnqueueReliableSend(peer.Id, ref resp);
    }

    private void OnCharacterListRequest(CharacterListRequest packet, NetPeer peer)
    {
        if (!_sessions.TryGetAccount(peer, out var accountId))
        {
            var empty = new CharacterListResponse { Characters = Array.Empty<CharacterData>() };
            _sender.EnqueueReliableSend(peer.Id, ref empty);
            return;
        }
        var list = _characters.GetByAccountAsDto(accountId).ToArray();
        var resp = new CharacterListResponse { Characters = list };
        _sender.EnqueueReliableSend(peer.Id, ref resp);
    }

    private void OnCharacterCreationRequest(CharacterCreationRequest packet, NetPeer peer)
    {
        if (!_sessions.TryGetAccount(peer, out var accountId))
        {
            var denied = new CharacterCreationResponse { Success = false, Message = "Not logged" };
            _sender.EnqueueReliableSend(peer.Id, ref denied);
            return;
        }
        if (_characters.CountByAccount(accountId) >= CharacterConstants.MaxCharacterCount)
        {
            var denied = new CharacterCreationResponse { Success = false, Message = $"You can only have up to {CharacterConstants.MaxCharacterCount} characters." };
            _sender.EnqueueReliableSend(peer.Id, ref denied);
            return;
        }
        var name = (packet.Name ?? string.Empty).Trim();
        if (name.Length < CharacterConstants.MinCharacterNameLength || name.Length > CharacterConstants.MaxCharacterNameLength)
        {
            var denied = new CharacterCreationResponse { Success = false, Message = $"Name must be between {CharacterConstants.MinCharacterNameLength} and {CharacterConstants.MaxCharacterNameLength} characters." };
            _sender.EnqueueReliableSend(peer.Id, ref denied);
            return;
        }
        if (!Regex.IsMatch(name, CharacterConstants.NameRegexPattern))
        {
            var denied = new CharacterCreationResponse { Success = false, Message = "Name contains invalid characters or spacing." };
            _sender.EnqueueReliableSend(peer.Id, ref denied);
            return;
        }
        if (_characters.NameExists(name))
        {
            var denied = new CharacterCreationResponse { Success = false, Message = "Character name already in use." };
            _sender.EnqueueReliableSend(peer.Id, ref denied);
            return;
        }

        var ch = _characters.CreateAsync(accountId, name, packet.Vocation, packet.Gender);
        var dto = _characters.ToDto(ch);
        var ok = new CharacterCreationResponse { Success = true, Message = "Character created", Character = dto };
        _sender.EnqueueReliableSend(peer.Id, ref ok);
    }

    private void OnCharacterSelectionRequest(CharacterSelectionRequest packet, NetPeer peer)
    {
        if (!_sessions.TryGetAccount(peer, out var accountId))
        {
            var denied = new CharacterSelectionResponse { Success = false, Message = "Not logged" };
            _sender.EnqueueReliableSend(peer.Id, ref denied);
            return;
        }
        if (!_characters.GetById(packet.CharacterId, out var ch) || ch!.AccountId != accountId)
        {
            var denied = new CharacterSelectionResponse { Success = false, Message = "Character not found" };
            _sender.EnqueueReliableSend(peer.Id, ref denied);
            return;
        }
        var ok = new CharacterSelectionResponse { Success = true, Message = "Character selected", Character = _characters.ToDto(ch!) };
        _sender.EnqueueReliableSend(peer.Id, ref ok);
    }

    public override void Dispose()
    {
        foreach (var s in _subs) s.Dispose();
        _subs.Clear();
        base.Dispose();
    }
}
