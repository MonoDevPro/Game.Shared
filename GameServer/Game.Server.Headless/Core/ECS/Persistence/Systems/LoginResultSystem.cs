using Arch.Core;
using Arch.System;
using Game.Server.Headless.Infrastructure.Repositories;
using GameServer.Infrastructure.EfCore.Worker;
using Microsoft.Extensions.Logging;
using Shared.Core.Network.Transport;
using Shared.Features.MainMenu.Account.AccountLogin;

namespace Game.Server.Headless.Core.ECS.Persistence.Systems;

public sealed class LoginResultSystem(
    World world,
    IBackgroundPersistence persistence,
    SessionService sessions,
    NetworkSender sender,
    ILogger<LoginResultSystem> logger) : BaseSystem<World, float>(world)
{
    public override void Update(in float t)
    {
        var reader = persistence.LoginResults;
        while (reader.TryRead(out var res))
        {
            if (!res.Success)
            {
                var fail = new AccountLoginResponse { Success = false, Message = res.ErrorMessage ?? "Login failed" };
                sender.EnqueueReliableSend(res.SenderPeer, ref fail);
                continue;
            }

            if (res.AccountId.HasValue)
                sessions.Bind(res.SenderPeer, res.AccountId.Value);

            var ok = new AccountLoginResponse { Success = true, Message = "Login ok" };
            sender.EnqueueReliableSend(res.SenderPeer, ref ok);
            logger.LogInformation("Login ok (cmd {Cmd}) for peer {Peer} account {Acc}", res.CommandId, res.SenderPeer, res.AccountId);
        }
    }
}