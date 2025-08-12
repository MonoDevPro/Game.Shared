using LiteNetLib;

namespace GameClient.Features.Game.Player.Events;

public struct ServerConnectedEvent
{
    /// <summary>
    /// O peer do servidor ao qual o cliente está conectado.
    /// </summary>
    public NetPeer Peer;
}

public struct ServerDisconnectedEvent
{
    /// <summary>
    /// O peer do servidor do qual o cliente foi desconectado.
    /// </summary>
    public NetPeer Peer;
    /// <summary>
    /// A razão pela qual o cliente foi desconectado.
    /// </summary>
    public string Reason;
}