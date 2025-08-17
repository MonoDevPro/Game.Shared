using Game.Core.Common.Enums;
using LiteNetLib.Utils;

namespace Shared.MainMenu.Character.CharacterCreation;

public struct CharacterCreationRequest : INetSerializable
{
    public string Name;
    public VocationEnum Vocation;
    public GenderEnum Gender;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Name);
        writer.Put((byte)Vocation);
        writer.Put((byte)Gender);
    }

    public void Deserialize(NetDataReader reader)
    {
        Name = reader.GetString();
        Vocation = (VocationEnum)reader.GetByte();
        Gender = (GenderEnum)reader.GetByte();
    }
}