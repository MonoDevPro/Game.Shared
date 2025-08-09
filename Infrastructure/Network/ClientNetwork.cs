using GameClient.Infrastructure.Events;
using Godot;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Config;
using Shared.Infrastructure.Network.Repository;
using Shared.Infrastructure.Network.Transport;

namespace GameClient.Infrastructure.Network;

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
        => NetworkEvents.RaiseConnectedToServer();
    
    private void OnServerDisconnected(NetPeer peer, string reason)
        => NetworkEvents.RaiseDisconnectedFromServer();

    public override void Dispose()
    {
        PeerRepository.PeerConnected -= OnServerConnected;
        PeerRepository.PeerDisconnected -= OnServerDisconnected;
        base.Dispose();
    }
}