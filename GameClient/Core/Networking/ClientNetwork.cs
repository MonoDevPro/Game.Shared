using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Network;
using Shared.Network.Repository;
using Shared.Network.Transport;

namespace GameClient.Core.Networking;

/// <summary>
/// Client-specific adapter implementing connection logic.
/// </summary>
public sealed class ClientNetwork(
    NetManager netManager, NetworkSender sender, NetworkReceiver receiver, PeerRepository peerRepository,
    ILogger<ClientNetwork> logger, INetLogger liteNetLogger) 
    : NetworkManager(netManager, sender, receiver, peerRepository, logger, liteNetLogger)
{
    private string Host => NetworkConfigurations.Host;
    private int Port => NetworkConfigurations.Port;
    private string SecretKey => NetworkConfigurations.SecretKey;

    public override NetworkModeEnum NetworkMode { get; } = NetworkModeEnum.Client;

    public override void Start()
    {
        if (!IsRunning)
            NetManager.Start();
        
        if (!peerRepository.IsConnected(0))
            NetManager.Connect(Host, Port, SecretKey);
    }
    
    public override void Dispose()
    {
        base.Dispose();
    }
}