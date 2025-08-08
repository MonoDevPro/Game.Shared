using Godot;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Config;

namespace Game.Shared.Client.Infrastructure.Network;

/// <summary>
/// Client-specific adapter implementing connection logic.
/// </summary>
public sealed class ClientNetwork(ILoggerFactory factory) : NetworkManager(factory)
{
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
}
