using Game.Core.Entities.Common.Enums;
using LiteNetLib.Utils;

namespace Shared.MainMenu.Character;

public struct CharacterData : INetSerializable
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