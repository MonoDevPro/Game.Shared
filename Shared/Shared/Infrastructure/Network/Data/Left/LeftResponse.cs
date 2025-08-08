using LiteNetLib.Utils;

namespace Shared.Infrastructure.Network.Data.Left;

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
