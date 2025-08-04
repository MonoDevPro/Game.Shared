using Game.Shared.Shared.Infrastructure.Network.Config;
using Game.Shared.Shared.Infrastructure.Network.Repository;
using Game.Shared.Shared.Infrastructure.Network.Transport;
using Godot;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Game.Shared.Shared.Infrastructure.Network;

public abstract partial class NetworkManager : Node
{
    protected readonly NetManager NetManager;
    protected readonly EventBasedNetListener _listener;
    protected readonly NetPacketProcessor Processor;

    public virtual NetworkSender Sender { get; }
    public virtual NetworkReceiver Receiver { get; }
    public virtual PeerRepositoryRef PeerRepository { get; }
    
    protected bool IsRunning => NetManager.IsRunning;
    private int MaxStringLength => NetworkConfigurations.MaxStringLength;
    
    protected NetworkManager()
    {
        _listener = new EventBasedNetListener();
        NetManager = new NetManager(_listener);
        Processor = new NetPacketProcessor(MaxStringLength);
        
        Sender = new NetworkSender(NetManager, Processor);
        Receiver = new NetworkReceiver(Processor, _listener);
        PeerRepository = new PeerRepositoryRef(_listener, NetManager);
    }
    
    public abstract void Start();

    public virtual void Stop()
    {
        if (!IsRunning)
        {
            GD.Print("[NetworkManager] Not running, skipping stop.");
            return;
        }
        NetManager.DisconnectAll();
        PeerRepository.Stop();
        NetManager.Stop();
    }
    
    /// <summary>
    /// Poll de eventos de rede - deve ser chamado pelo LambdaNetReceiveSystem
    /// </summary>
    public void PollEvents()
    {
        NetManager.PollEvents();
    }

    public override void _ExitTree()
    {
        Stop();
        DisposeResources();
        base._ExitTree();
    }

    protected virtual new void DisposeResources()
    {
        Receiver?.Dispose();
        NetManager?.Stop();
        GD.Print("[NetworkManager] Resources disposed");
    }
}
