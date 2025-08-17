using Arch.Bus;
using Arch.Core;
using Arch.System;
using LiteNetLib;
using Shared.Network;
using Shared.Network.Packets.Game.Chat;

// Outros usings...

namespace GameClient.Core.ECS.Systems.Receive;

public partial class NetworkToChatSystem : BaseSystem<World, float>
{
    private readonly NetworkManager _networkManager;

    public NetworkToChatSystem(World world, NetworkManager networkManager) : base(world)
    {
        _networkManager = networkManager;
        
        // Continua a ouvir a rede como antes
        var receiver = _networkManager.Receiver;
        receiver.RegisterMessageHandler<ChatMessageBroadcast>(OnChatMessageReceived);
        
        Hook();
    }
    
    private void OnChatMessageReceived(ChatMessageBroadcast packet, NetPeer peer)
    {
        // Levanta um aviso no quadro a dizer "mensagem recebida!"
        EventBus.Send(ref packet);
    }
    
    [Event(order: 0)]
    public void OnMessageSendRequest(ref ChatMessageRequest chatMessageRequest)
    {
        _networkManager.Sender.EnqueueReliableSend(0, ref chatMessageRequest); // Envia ao servidor
    }
    
    public override void Dispose()
    {
        Unhook();
        base.Dispose();
    }
}