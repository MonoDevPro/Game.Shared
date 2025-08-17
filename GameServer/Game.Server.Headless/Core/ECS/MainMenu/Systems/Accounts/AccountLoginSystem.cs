using Arch.Core;
using Game.Server.Headless.Infrastructure.Repositories;
using GameServer.Infrastructure.EfCore.Worker;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Network.Packets.MainMenu.Account.AccountLogin;
using Shared.Network.Transport;

namespace Game.Server.Headless.Core.ECS.MainMenu.Systems.Accounts;

public sealed class AccountLoginSystem : BaseMainMenuSystem
{
    public AccountLoginSystem(
        World world,
        ILogger<AccountLoginSystem> logger,
        NetworkReceiver receiver,
        NetworkSender sender,
        SessionService sessions,
        IBackgroundPersistence persistence) : base(world, logger, receiver, sender, sessions, persistence)
    {
        RegisterHandler<AccountLoginRequest>(OnAccountLoginRequest);
    }

    public override void Update(in float t)
    {
        // Drain results without blocking the loop
        var reader = Persistence.LoginResults;
        while (reader.TryRead(out var res))
        {
            if (!res.Success)
            {
                var fail = new AccountLoginResponse { Success = false, Message = res.ErrorMessage ?? "Login failed" };
                Sender.EnqueueReliableSend(res.SenderPeer, ref fail);
                continue;
            }

            if (res.AccountId.HasValue)
                Sessions.Bind(res.SenderPeer, res.AccountId.Value);

            var ok = new AccountLoginResponse { Success = true, Message = "Login ok" };
            Sender.EnqueueReliableSend(res.SenderPeer, ref ok);
            Logger.LogInformation("Login ok (cmd {Cmd}) for peer {Peer} account {Acc}", res.CommandId, res.SenderPeer, res.AccountId);
        }
    }

    private void OnAccountLoginRequest(AccountLoginRequest packet, NetPeer peer)
    {
        Logger.LogInformation("AccountLogin from {Peer}", peer.Id);
        // Offload to worker; it will perform validation and hashing/verification
        // Offload to BackgroundPersistence/DatabaseWorker: enqueue login request
        var cmdId = Guid.NewGuid();
        var req = new LoginRequest(cmdId, packet.Username ?? string.Empty, packet.Password ?? string.Empty, peer.Id);
        var t = Persistence.EnqueueLoginAsync(req).AsTask();
        t.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && !task.Result)
                Logger.LogWarning("Login queue full for peer {PeerId}", peer.Id);
            else if (task.IsFaulted)
                Logger.LogError(task.Exception, "Error enqueuing login for peer {PeerId}", peer.Id);
        }, TaskScheduler.Default);
    }
}