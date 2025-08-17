using LiteNetLib.Utils;

namespace Shared.Network.Packets.Game.Player;

public struct LeftGameRequest : INetSerializable
{
    public void Serialize(NetDataWriter writer)
    {
        // No data to serialize for left message
    }

    public void Deserialize(NetDataReader reader)
    {
        // No data to deserialize for left message
    }
}
