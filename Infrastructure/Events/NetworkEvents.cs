using System;

namespace GameClient.Infrastructure.Events;

public static class NetworkEvents
{
    /// <summary>
    /// Um quadro de avisos central para eventos de rede no cliente.
    /// </summary>
    public static event Action OnConnectedToServer;
    public static void RaiseConnectedToServer() => OnConnectedToServer?.Invoke();

    /// <summary>
    /// Evento disparado quando o cliente se desconecta do servidor.
    /// </summary>
    public static event Action OnDisconnectedFromServer;
    public static void RaiseDisconnectedFromServer() => OnDisconnectedFromServer?.Invoke();
}