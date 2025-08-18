using Arch.Core;
using Arch.System;
using Game.Server.Headless.Core.ECS.Game.Components.Intents;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Network;
using Shared.Network.Packets.Game.Player;

namespace Game.Server.Headless.Core.ECS.Game.Systems.Adapters;

/// <summary>
/// Adapter de rede: recebe EnterGameRequest e enfileira EnterGameIntent no ECS.
/// </summary>
public class EnterGameAdapterSystem : BaseSystem<World, float>
{
    private readonly ILogger<EnterGameAdapterSystem> _logger;
    private readonly List<IDisposable> _subs = [];

    public EnterGameAdapterSystem(World world, NetworkManager network, ILogger<EnterGameAdapterSystem> logger) : base(world)
    {
        _logger = logger;
        _subs.Add(network.Receiver.RegisterMessageHandler<EnterGameRequest>(OnEnterGameRequest));
    }

    private void OnEnterGameRequest(EnterGameRequest packet, NetPeer peer)
    {
        var intent = new EnterGameIntent { PeerId = peer.Id, CharacterId = packet.CharacterId };
        World.Create(intent);
        _logger.LogDebug("EnterGameIntent enqueued for peer {Peer} character {Char}", peer.Id, packet.CharacterId);
    }

    public override void Dispose()
    {
        foreach (var s in _subs) s.Dispose();
        _subs.Clear();
        base.Dispose();
    }
}
