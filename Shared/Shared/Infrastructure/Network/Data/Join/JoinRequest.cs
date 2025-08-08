using LiteNetLib.Utils;
using Shared.Core.Enums;

namespace Shared.Infrastructure.Network.Data.Join;

public struct JoinRequest : INetSerializable
{
    public string Name;
    public GenderEnum Gender;
    public VocationEnum Vocation;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Name);
        writer.Put((byte)Gender);
        writer.Put((byte)Vocation);
    }

    public void Deserialize(NetDataReader reader)
    {
        Name = reader.GetString();
        Gender = (GenderEnum)reader.GetByte();
        Vocation = (VocationEnum)reader.GetByte();
    }
}
