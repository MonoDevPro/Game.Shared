using System;
using Shared.Infrastructure.Network.Data.Chat;

namespace GameClient.Infrastructure.Events;

public static class ChatEvents
{
    // Evento para quando o jogador quer ENVIAR uma mensagem
    // A UI levanta este evento. O sistema de rede ouve-o.
    public static event Action<string> OnSendMessageRequested;
    public static void RaiseSendMessageRequested(string message) => OnSendMessageRequested?.Invoke(message);

    // Evento para quando uma mensagem é RECEBIDA
    // O sistema de rede levanta este evento. A UI ouve-o.
    public static event Action<ChatMessageBroadcast> OnChatMessageReceived;
    public static void RaiseChatMessageReceived(ChatMessageBroadcast packet) => OnChatMessageReceived?.Invoke(packet);
}