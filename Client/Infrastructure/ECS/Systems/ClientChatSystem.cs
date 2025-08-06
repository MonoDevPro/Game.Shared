using Arch.Core;
using Arch.System;
using Game.Shared.Client.Presentation.UI.Chat;
using Game.Shared.Shared.Infrastructure.Network;
using Game.Shared.Shared.Infrastructure.Network.Data.Chat;
using Godot;
using LiteNetLib;

// Outros usings...

namespace Game.Shared.Client.Infrastructure.ECS.Systems;

public partial class ClientChatSystem : BaseSystem<World, float>
{
    private readonly NetworkManager _networkManager;
    private readonly ChatUI _chatUI;

    public ClientChatSystem(World world, NetworkManager networkManager, ChatUI chatUI) : base(world)
    {
        _networkManager = networkManager;
        _chatUI = chatUI;
        
        // Se inscreve para receber mensagens do servidor
        _networkManager.Receiver
            .RegisterMessageHandler<ChatMessageBroadcast>(OnChatMessageBroadcast);
            
        // Se inscreve no sinal da UI para enviar mensagens
        _chatUI.MessageSent += OnMessageSendRequest;
    }

    private void OnMessageSendRequest(string message)
    {
        var request = new ChatMessageRequest { Message = message };
        _networkManager.Sender.EnqueueReliableSend(0, ref request); // Envia ao servidor
    }

    private void OnChatMessageBroadcast(ChatMessageBroadcast packet, NetPeer peer)
    {
        // Atualiza a UI com a mensagem recebida
        _chatUI.AddChatMessage(packet.SenderName, packet.Message, Colors.White);
    }
}