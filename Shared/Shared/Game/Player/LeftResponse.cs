using LiteNetLib.Utils;

namespace Shared.Game.Player;

public struct LeftResponse : INetSerializable
{
    public int NetId { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
    }
}
