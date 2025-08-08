using LiteNetLib;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Config;

namespace Game.Server.Headless.Infrastructure.Network;

/// <summary>
/// Client-specific adapter implementing connection logic.
/// </summary>
public class ServerNetwork(ILoggerFactory factory) : NetworkManager(factory)
{
    private readonly ILogger<NetworkManager> _logger = factory.CreateLogger<NetworkManager>();
    
    public override void Start()
    {
        if (IsRunning)
        {
            _logger.LogInformation("[ServerNetwork] Already running, skipping start.");
            return;
        }
        
        var port = NetworkConfigurations.Port;
        
        NetManager.Start(port);
    }
}
