using Arch.Bus;
using GameClient.Features.Player.Events;
using Godot;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Core.Common.Constants;
using Shared.Core.Network;
using Shared.Core.Network.Repository;
using Shared.Core.Network.Transport;

namespace GameClient.Core.Networking;

/// <summary>
/// Client-specific adapter implementing connection logic.
/// </summary>
public sealed class ClientNetwork : NetworkManager
{
    /// <summary>
    /// Client-specific adapter implementing connection logic.
    /// </summary>
    public ClientNetwork(NetManager netManager,
        NetworkSender sender,
        NetworkReceiver receiver,
        PeerRepository peerRepository,
        ILogger<ClientNetwork> logger,
        INetLogger liteNetLogger) : base(netManager, sender, receiver, peerRepository, logger, liteNetLogger)
    {
        PeerRepository.PeerConnected += OnServerConnected;
        PeerRepository.PeerDisconnected += OnServerDisconnected;
    }

    private string Host => NetworkConfigurations.Host;
    private int Port => NetworkConfigurations.Port;
    private string SecretKey => NetworkConfigurations.SecretKey;

    public override NetworkModeEnum NetworkMode { get; } = NetworkModeEnum.Client;

    public override void Start()
    {
        if (IsRunning)
        {
            GD.Print("[ClientNetwork] Already running, skipping start.");
            return;
        }
        
        NetManager.Start();
        NetManager.Connect(Host, Port, SecretKey);
    }
    
    private void OnServerConnected(NetPeer peer)
    {
        var @event = new ServerConnectedEvent { Peer = peer };
        EventBus.Send(ref @event);
    }

    private void OnServerDisconnected(NetPeer peer, string reason)
    {
        var @event = new ServerDisconnectedEvent { Peer = peer, Reason = reason};
        EventBus.Send(ref @event);
    }

    public override void Dispose()
    {
        PeerRepository.PeerConnected -= OnServerConnected;
        PeerRepository.PeerDisconnected -= OnServerDisconnected;
        base.Dispose();
    }
}