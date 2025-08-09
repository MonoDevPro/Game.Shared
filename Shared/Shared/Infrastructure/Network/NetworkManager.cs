using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Network.Config;
using Shared.Infrastructure.Network.Repository;
using Shared.Infrastructure.Network.Transport;

namespace Shared.Infrastructure.Network;

public abstract class NetworkManager : IDisposable
{
    public NetworkSender Sender { get; }
    public NetworkReceiver Receiver { get; }
    public PeerRepository PeerRepository { get; }
    
    protected readonly NetManager NetManager;
    public bool IsRunning => NetManager.IsRunning;
    
    private readonly ILogger<NetworkManager> _logger;
    protected NetworkManager(
        NetManager netManager,
        NetworkSender sender,
        NetworkReceiver receiver,
        PeerRepository peerRepository,
        ILogger<NetworkManager> logger,
        INetLogger loggerNetLibLogger)
    {
        NetManager = netManager;
        Sender = sender;
        Receiver = receiver;
        PeerRepository = peerRepository;
        _logger = logger;
        
        NetDebug.Logger = loggerNetLibLogger;
    }
    
    public abstract void Start();

    public virtual void Stop()
    {
        if (!IsRunning)
        {
            _logger.LogInformation("[NetworkManager] Not running, skipping stop.");
            return;
        }
        NetManager.DisconnectAll();
        NetManager.Stop();
    }
    
    /// <summary>
    /// Poll de eventos de rede - deve ser chamado pelo LambdaNetReceiveSystem
    /// </summary>
    public void PollEvents()
    {
        NetManager.PollEvents();
    }
    
    public virtual void Dispose()
    {
        Stop();
        Sender.Dispose();
        Receiver.Dispose();
        PeerRepository.Dispose();
    }

}