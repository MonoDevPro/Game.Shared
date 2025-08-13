using LiteNetLib.Utils;
using Shared.Core.Common.Enums;

namespace Shared.Features.MainMenu.Character;

public struct CharacterDto : INetSerializable
{
    public int CharacterId;
    public string Name;
    public VocationEnum Vocation;
    public GenderEnum Gender;
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharacterId);
        writer.Put(Name);
        writer.Put((byte)Vocation);
        writer.Put((byte)Gender);
    }

    public void Deserialize(NetDataReader reader)
    {
        CharacterId = reader.GetInt();
        Name = reader.GetString();
        Vocation = (VocationEnum)reader.GetByte();
        Gender = (GenderEnum)reader.GetByte();
    }
}