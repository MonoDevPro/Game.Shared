using Arch.Core;
using Arch.System;
using Game.Server.Headless.Infrastructure.Repositories;
using GameServer.Infrastructure.EfCore.Worker;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Shared.Network.Transport;

namespace Game.Server.Headless.Core.ECS.MainMenu.Systems;

public abstract class BaseMainMenuSystem(
    World world,
    ILogger logger,
    NetworkReceiver receiver,
    NetworkSender sender,
    SessionService sessions,
    IBackgroundPersistence persistence)
    : BaseSystem<World, float>(world)
{
    protected readonly ILogger Logger = logger;
    protected readonly NetworkSender Sender = sender;
    protected readonly SessionService Sessions = sessions;
    protected readonly IBackgroundPersistence Persistence = persistence;
    private readonly List<IDisposable> _subs = [];

    protected void RegisterHandler<T>(Action<T, NetPeer> handler) where T : struct, INetSerializable
    {
        var sub = receiver.RegisterMessageHandler<T>(handler);
        _subs.Add(sub);
    }

    public override void Dispose()
    {
        foreach (var s in _subs) s.Dispose();
        _subs.Clear();
        base.Dispose();
    }

}