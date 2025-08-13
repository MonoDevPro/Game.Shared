using LiteNetLib.Utils;

namespace Shared.Features.MainMenu.Character.CharacterList;

public struct CharacterListRequest : INetSerializable
{
    public void Serialize(NetDataWriter writer)
    {
        // No data to serialize
    }

    public void Deserialize(NetDataReader reader)
    {
        // No data to deserialize
    }
}