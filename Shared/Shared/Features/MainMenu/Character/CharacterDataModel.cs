using LiteNetLib.Utils;
using Shared.Core.Common.Enums;

namespace Shared.Features.MainMenu.Character;

public struct CharacterDataModel : INetSerializable
{
    public int CharacterId { get; set; }
    public string Name { get; set; }
    public VocationEnum Vocation { get; set; }
    public GenderEnum Gender { get; set; }
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