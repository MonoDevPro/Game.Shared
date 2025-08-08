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
    protected bool IsRunning => NetManager.IsRunning;
    private int MaxStringLength => NetworkConfigurations.MaxStringLength;
    
    private readonly ILogger<NetworkManager> _logger;
    protected NetworkManager(ILoggerFactory factory)
    {
        _logger = factory.CreateLogger<NetworkManager>();
        NetDebug.Logger = new LiteNetLibLogger(factory.CreateLogger<LiteNetLibLogger>());
        
        var listener = new EventBasedNetListener();
        var processor = new NetPacketProcessor(MaxStringLength);
        
        NetManager = new NetManager(listener);
        Sender = new NetworkSender(NetManager, processor, factory.CreateLogger<NetworkSender>());
        Receiver = new NetworkReceiver(processor, listener, factory.CreateLogger<NetworkReceiver>());
        PeerRepository = new PeerRepository(listener, NetManager, factory.CreateLogger<PeerRepository>());
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