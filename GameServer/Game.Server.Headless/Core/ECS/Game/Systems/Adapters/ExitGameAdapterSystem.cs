using Arch.Core;
using Arch.System;
using Game.Server.Headless.Core.ECS.Game.Components;
using Game.Server.Headless.Core.ECS.Game.Components.Intents;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Network;
using Shared.Network.Packets.Game.Player;
using Shared.Network.Transport;

namespace Game.Server.Headless.Core.ECS.Game.Systems.Adapters;

/// <summary>
/// Adapter de rede: recebe EnterGameRequest e enfileira EnterGameIntent no ECS.
/// </summary>
public class ExitGameAdapterSystem : BaseSystem<World, float>
{
    private readonly ILogger<ExitGameAdapterSystem> _logger;
    private readonly List<IDisposable> _subs = [];

    public ExitGameAdapterSystem(World world, NetworkManager network, ILogger<ExitGameAdapterSystem> logger) : base(world)
    {
        _logger = logger;
        
        network.PeerRepository.PeerDisconnected += OnPeerDisconnected;
        
        _subs.Add(new DisposableAction(() =>
        {
            network.PeerRepository.PeerDisconnected -= OnPeerDisconnected;
            _logger.LogDebug("Unregistered PeerDisconnected handler in ExitGameAdapterSystem.");
        }));
        
        _subs.Add(network.Receiver.RegisterMessageHandler<ExitGameRequest>(OnExitGameRequest));
    }
    
    private void OnExitGameRequest(ExitGameRequest packet, NetPeer peer)
    {
        var intent = new ExitGameIntent { PeerId = peer.Id, CharacterId = packet.CharacterId, Reason = "Requested by client" };
        World.Create(intent);
        _logger.LogDebug("ExitGameIntent enqueued for peer {Peer} character {Char}", peer.Id, packet.CharacterId);
    }

    /// <summary>
    /// Handler para quando a conexão do jogador cai (causa: desconexão).
    /// </summary>
    private void OnPeerDisconnected(NetPeer peer, string reason)
    {
        _logger.LogInformation("Peer {PeerId} desconectado. Motivo: {Reason}. Processando logout.", peer.Id, reason);
        var intent = new ExitGameIntent { PeerId = peer.Id, Reason = reason };
        World.Create(intent);
        _logger.LogDebug("ExitGameIntent enqueued for peer {Peer} character {Char}", peer.Id, 0);
    }

    public override void Dispose()
    {
        foreach (var s in _subs) s.Dispose();
        _subs.Clear();
        base.Dispose();
    }
}
