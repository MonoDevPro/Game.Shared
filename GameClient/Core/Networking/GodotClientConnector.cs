using Shared.Core.Network;

namespace GameClient.Core.Networking;

public interface IClientConnector
{
    void Connect(); // chamado no main thread
}

public class GodotClientConnector(NetworkManager networkManager) : IClientConnector
{
    public void Connect() => networkManager.Start(); // ou ClientNetwork.Connect(host,port,token)
}