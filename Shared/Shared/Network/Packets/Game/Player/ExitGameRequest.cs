using LiteNetLib.Utils;

namespace Shared.Network.Packets.Game.Player;

public struct ExitGameRequest : INetSerializable
{
    public int CharacterId;
    
    public void Serialize(NetDataWriter writer)
    {
    }

    public void Deserialize(NetDataReader reader)
    {
    }
}
