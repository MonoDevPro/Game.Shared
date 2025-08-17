using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Network;
using Shared.Network.Repository;
using Shared.Network.Transport;

namespace Game.Server.Headless.Infrastructure.Network;

/// <summary>
/// Client-specific adapter implementing connection logic.
/// </summary>
public class ServerNetwork(NetManager netManager,
    NetworkSender sender,
    NetworkReceiver receiver,
    PeerRepository peerRepository,
    ILogger<ServerNetwork> logger,
    INetLogger liteNetLogger) : NetworkManager(netManager, sender, receiver, peerRepository, logger, liteNetLogger)
{
    public override NetworkModeEnum NetworkMode { get; } = NetworkModeEnum.Server;

    public override void Start()
    {
        if (IsRunning)
        {
            logger.LogInformation("[ServerNetwork] Already running, skipping start.");
            return;
        }
        
        var port = NetworkConfigurations.Port;
        
        NetManager.Start(port);
    }
}
