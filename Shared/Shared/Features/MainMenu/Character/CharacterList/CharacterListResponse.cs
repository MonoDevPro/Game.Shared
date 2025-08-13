using LiteNetLib.Utils;
using Shared.Core.Common.Enums;

namespace Shared.Features.MainMenu.Character.CharacterList;

public struct CharacterListResponse : INetSerializable
{
    public CharacterDto[] Characters;

    public void Serialize(NetDataWriter writer)
    {
        writer.PutArray<CharacterDto>(Characters);
    }

    public void Deserialize(NetDataReader reader)
    {
        Characters = reader.GetArray<CharacterDto>();
    }
}