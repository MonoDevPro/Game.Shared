using LiteNetLib.Utils;
using Shared.Core.Common.Enums;

namespace Shared.Features.MainMenu.Character.CharacterList;

public struct CharacterListResponse : INetSerializable
{
    public CharacterDataModel[] Characters;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Characters.Length);
        foreach (var character in Characters)
        {
            writer.Put(character.CharacterId);
            writer.Put(character.Name);
            writer.Put((byte)character.Vocation);
            writer.Put((byte)character.Gender);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        var length = reader.GetInt();
        Characters = new CharacterDataModel[length];
        for (var i = 0; i < length; i++)
        {
            Characters[i] = new CharacterDataModel
            {
                CharacterId = reader.GetInt(),
                Name = reader.GetString(),
                Vocation = (VocationEnum)reader.GetByte(),
                Gender = (GenderEnum)reader.GetByte()
            };
        }
    }
}