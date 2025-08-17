using Arch.Core;
using Game.Server.Headless.Infrastructure.Repositories;
using GameServer.Infrastructure.EfCore.Worker;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Network.Packets.MainMenu.Account.AccountCreation;
using Shared.Network.Transport;

namespace Game.Server.Headless.Core.ECS.MainMenu.Systems.Accounts;

public sealed class AccountCreationSystem : BaseMainMenuSystem
{
    public AccountCreationSystem(
        World world,
        ILogger<AccountCreationSystem> logger,
        NetworkReceiver receiver,
        NetworkSender sender,
        SessionService sessions,
        IBackgroundPersistence persistence) : base(world, logger, receiver, sender, sessions, persistence)
    {
        RegisterHandler<AccountCreationRequest>(OnAccountCreationRequest);
    }

    public override void Update(in float t)
    {
        // Drain results without blocking the loop
        // 1) Account creation
        var accReader = Persistence.AccountCreationResults;
        while (accReader.TryRead(out var accRes))
        {
            if (accRes.Success)
            {
                var ok = new AccountCreationResponse { Success = true, Message = "Account created" };
                Sender.EnqueueReliableSend(accRes.SenderPeer, ref ok);
            }
            else
            {
                var fail = new AccountCreationResponse { Success = false, Message = accRes.ErrorMessage ?? "Creation failed" };
                Sender.EnqueueReliableSend(accRes.SenderPeer, ref fail);
            }
        }
    }

    private void OnAccountCreationRequest(AccountCreationRequest packet, NetPeer peer)
    {
        Logger.LogInformation("AccountCreation from {Peer}", peer.Id);
        // Offload to background worker (all validations happen in the worker)
        var cmdId = Guid.NewGuid();
        var req = new AccountCreationRequestMsg(cmdId, packet.Username, packet.Email, packet.Password, peer.Id);
        var t = Persistence.EnqueueAccountCreationAsync(req).AsTask();
        t.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && !task.Result)
                Logger.LogWarning("AccountCreation queue full for peer {PeerId}", peer.Id);
            else if (task.IsFaulted)
                Logger.LogError(task.Exception, "Error enqueuing account creation for peer {PeerId}", peer.Id);
        }, TaskScheduler.Default);
    }
}