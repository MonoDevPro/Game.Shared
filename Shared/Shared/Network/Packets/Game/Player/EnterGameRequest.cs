using LiteNetLib.Utils;

namespace Shared.Network.Packets.Game.Player;

public struct EnterGameRequest : INetSerializable
{
    public int CharacterId;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharacterId);
    }

    public void Deserialize(NetDataReader reader)
    {
        CharacterId = reader.GetInt();
    }
}
