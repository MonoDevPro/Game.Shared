using Arch.Core;
using Arch.System;
using Game.Server.Headless.Infrastructure.Repositories;
using GameServer.Infrastructure.EfCore.Worker;
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
    private readonly SessionService _sessions;
    private readonly List<IDisposable> _subs = [];
    private readonly IBackgroundPersistence _persistence;

    public MainMenuReceiveSystem(
        World world,
        ILogger<MainMenuReceiveSystem> logger,
    NetworkReceiver receiver,
    NetworkSender sender,
    SessionService sessions,
        IBackgroundPersistence persistence) : base(world)
    {
        _logger = logger;
        _receiver = receiver;
        _sender = sender;
        _sessions = sessions;
        _persistence = persistence;

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
        // Drain results without blocking the loop
        // 1) Account creation
        var accReader = _persistence.AccountCreationResults;
        while (accReader.TryRead(out var accRes))
        {
            if (accRes.Success)
            {
                var ok = new AccountCreationResponse { Success = true, Message = "Account created" };
                _sender.EnqueueReliableSend(accRes.SenderPeer, ref ok);
            }
            else
            {
                var fail = new AccountCreationResponse { Success = false, Message = accRes.ErrorMessage ?? "Creation failed" };
                _sender.EnqueueReliableSend(accRes.SenderPeer, ref fail);
            }
        }

        // 2) Character list
        var listReader = _persistence.CharacterListResults;
        while (listReader.TryRead(out var listRes))
        {
            var arr = listRes.Characters.Select(x => new CharacterData
            {
                CharacterId = x.CharacterId,
                Name = x.Name,
                Vocation = x.Vocation,
                Gender = x.Gender
            }).ToArray();
            var resp = new CharacterListResponse { Characters = arr };
            _sender.EnqueueReliableSend(listRes.SenderPeer, ref resp);
        }

        // 3) Character creation
        var creationReader = _persistence.CharacterCreationResults;
        while (creationReader.TryRead(out var cr))
        {
            if (!cr.Success || cr.Character is null)
            {
                var denied = new CharacterCreationResponse { Success = false, Message = cr.ErrorMessage ?? "Create failed" };
                _sender.EnqueueReliableSend(cr.SenderPeer, ref denied);
            }
            else
            {
                var dto = new CharacterData { CharacterId = cr.Character.CharacterId, Name = cr.Character.Name, Vocation = cr.Character.Vocation, Gender = cr.Character.Gender };
                var ok = new CharacterCreationResponse { Success = true, Message = "Character created", Character = dto };
                _sender.EnqueueReliableSend(cr.SenderPeer, ref ok);
            }
        }

        // 4) Character selection
        var selReader = _persistence.CharacterSelectionResults;
        while (selReader.TryRead(out var sr))
        {
            if (!sr.Success || sr.Character is null)
            {
                var denied = new CharacterSelectionResponse { Success = false, Message = sr.ErrorMessage ?? "Character not found" };
                _sender.EnqueueReliableSend(sr.SenderPeer, ref denied);
            }
            else
            {
                var dto = new CharacterData { CharacterId = sr.Character.CharacterId, Name = sr.Character.Name, Vocation = sr.Character.Vocation, Gender = sr.Character.Gender };
                // Persist selection into session for later EnterGame request
                _sessions.SetSelectedCharacter(sr.SenderPeer, dto);
                var ok = new CharacterSelectionResponse { Success = true, Message = "Character selected", Character = dto };
                _sender.EnqueueReliableSend(sr.SenderPeer, ref ok);
            }
        }
    }

    private void OnAccountCreationRequest(AccountCreationRequest packet, NetPeer peer)
    {
        _logger.LogInformation("AccountCreation from {Peer}", peer.Id);
        // Offload to background worker (all validations happen in the worker)
        var cmdId = Guid.NewGuid();
        var req = new AccountCreationRequestMsg(cmdId, packet.Username, packet.Email, packet.Password, peer.Id);
        var t = _persistence.EnqueueAccountCreationAsync(req).AsTask();
        t.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && !task.Result)
                _logger.LogWarning("AccountCreation queue full for peer {PeerId}", peer.Id);
            else if (task.IsFaulted)
                _logger.LogError(task.Exception, "Error enqueuing account creation for peer {PeerId}", peer.Id);
        }, TaskScheduler.Default);
    }

    private void OnAccountLoginRequest(AccountLoginRequest packet, NetPeer peer)
    {
        _logger.LogInformation("AccountLogin from {Peer}", peer.Id);
        // Offload to worker; it will perform validation and hashing/verification
        // Offload to BackgroundPersistence/DatabaseWorker: enqueue login request
        var cmdId = Guid.NewGuid();
        var req = new LoginRequest(cmdId, packet.Username ?? string.Empty, packet.Password ?? string.Empty, peer.Id);
        var t = _persistence.EnqueueLoginAsync(req).AsTask();
        t.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && !task.Result)
                _logger.LogWarning("Login queue full for peer {PeerId}", peer.Id);
            else if (task.IsFaulted)
                _logger.LogError(task.Exception, "Error enqueuing login for peer {PeerId}", peer.Id);
        }, TaskScheduler.Default);
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
        var cmdId = Guid.NewGuid();
        var req = new CharacterListRequestMsg(cmdId, accountId, peer.Id);
        var t = _persistence.EnqueueCharacterListAsync(req).AsTask();
        t.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && !task.Result)
                _logger.LogWarning("CharacterList queue full for peer {PeerId}", peer.Id);
            else if (task.IsFaulted)
                _logger.LogError(task.Exception, "Error enqueuing character list for peer {PeerId}", peer.Id);
        }, TaskScheduler.Default);
    }

    private void OnCharacterCreationRequest(CharacterCreationRequest packet, NetPeer peer)
    {
        if (!_sessions.TryGetAccount(peer, out var accountId))
        {
            var denied = new CharacterCreationResponse { Success = false, Message = "Not logged" };
            _sender.EnqueueReliableSend(peer.Id, ref denied);
            return;
        }
        var cmdId = Guid.NewGuid();
        // All name/format validations centralized in worker
        var req = new CharacterCreationRequestMsg(cmdId, accountId, packet.Name ?? string.Empty, packet.Vocation, packet.Gender, peer.Id);
        var t = _persistence.EnqueueCharacterCreationAsync(req).AsTask();
        t.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && !task.Result)
                _logger.LogWarning("CharacterCreation queue full for peer {PeerId}", peer.Id);
            else if (task.IsFaulted)
                _logger.LogError(task.Exception, "Error enqueuing character creation for peer {PeerId}", peer.Id);
        }, TaskScheduler.Default);
    }

    private void OnCharacterSelectionRequest(CharacterSelectionRequest packet, NetPeer peer)
    {
        if (!_sessions.TryGetAccount(peer, out var accountId))
        {
            var denied = new CharacterSelectionResponse { Success = false, Message = "Not logged" };
            _sender.EnqueueReliableSend(peer.Id, ref denied);
            return;
        }
        var cmdId = Guid.NewGuid();
        var req = new CharacterSelectionRequestMsg(cmdId, accountId, packet.CharacterId, peer.Id);
        var t = _persistence.EnqueueCharacterSelectionAsync(req).AsTask();
        t.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && !task.Result)
                _logger.LogWarning("CharacterSelection queue full for peer {PeerId}", peer.Id);
            else if (task.IsFaulted)
                _logger.LogError(task.Exception, "Error enqueuing character selection for peer {PeerId}", peer.Id);
        }, TaskScheduler.Default);
    }

    public override void Dispose()
    {
        foreach (var s in _subs) s.Dispose();
        _subs.Clear();
        base.Dispose();
    }
}