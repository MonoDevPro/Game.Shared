using LiteNetLib.Utils;

namespace Shared.Network.Packets.MainMenu.Character.CharacterList;

public struct CharacterListResponse : INetSerializable
{
    public CharacterData[] Characters;

    public void Serialize(NetDataWriter writer)
    {
        writer.PutArray<CharacterData>(Characters);
    }

    public void Deserialize(NetDataReader reader)
    {
        Characters = reader.GetArray<CharacterData>();
    }
}