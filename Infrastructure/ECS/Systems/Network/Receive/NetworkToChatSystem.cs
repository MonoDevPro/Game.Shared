using Arch.Core;
using Arch.System;
using GameClient.Infrastructure.Events;
using LiteNetLib;
using Shared.Infrastructure.Network;
using Shared.Infrastructure.Network.Data.Chat;

// Outros usings...

namespace GameClient.Infrastructure.ECS.Systems.Network.Receive;

public partial class NetworkToChatSystem : BaseSystem<World, float>
{
    private readonly NetworkManager _networkManager;

    public NetworkToChatSystem(World world, NetworkManager networkManager) : base(world)
    {
        _networkManager = networkManager;
        
        // Ouve o aviso "enviar mensagem" do quadro de avisos
        ChatEvents.OnSendMessageRequested += OnMessageSendRequest;
        
        // Continua a ouvir a rede como antes
        var receiver = _networkManager.Receiver;
        receiver.RegisterMessageHandler<ChatMessageBroadcast>(OnChatMessageBroadcast);
    }

    private void OnMessageSendRequest(string message)
    {
        var request = new ChatMessageRequest { Message = message };
        _networkManager.Sender.EnqueueReliableSend(0, ref request); // Envia ao servidor
    }

    private void OnChatMessageBroadcast(ChatMessageBroadcast packet, NetPeer peer)
    {
        // Levanta um aviso no quadro a dizer "mensagem recebida!"
        ChatEvents.RaiseChatMessageReceived(packet);
    }

    public override void Dispose()
    {
        ChatEvents.OnSendMessageRequested -= OnMessageSendRequest;
        base.Dispose();
    }
}