using Arch.Core;
using Game.Server.Headless.Infrastructure.Repositories;
using GameServer.Infrastructure.EfCore.Worker;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Network.Packets.MainMenu.Account.AccountLogout;
using Shared.Network.Transport;

namespace Game.Server.Headless.Core.ECS.MainMenu.Systems.Accounts;

public sealed class AccountLogoutSystem : BaseMainMenuSystem
{
    public AccountLogoutSystem(
        World world,
        ILogger<AccountLogoutSystem> logger,
        NetworkReceiver receiver,
        NetworkSender sender,
        SessionService sessions,
        IBackgroundPersistence persistence) : base(world, logger, receiver, sender, sessions, persistence)
    {
        RegisterHandler<AccountLogoutRequest>(OnAccountLogoutRequest);
    }

    private void OnAccountLogoutRequest(AccountLogoutRequest packet, NetPeer peer)
    {
        Logger.LogInformation("AccountLogout from {Peer}", peer.Id);
        Sessions.Unbind(peer);
        var resp = new AccountLogoutResponse { Success = true, Message = "Logged out" };
        Sender.EnqueueReliableSend(peer.Id, ref resp);
    }
}