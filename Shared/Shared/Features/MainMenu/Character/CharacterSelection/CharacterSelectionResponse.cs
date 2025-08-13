using LiteNetLib.Utils;

namespace Shared.Features.MainMenu.Character.CharacterSelection;

public struct CharacterSelectionResponse : INetSerializable
{
    public bool Success;
    public string Message;
    public CharacterDto Character;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Success);
        writer.Put(Message);
        writer.Put<CharacterDto>(Character);
    }

    public void Deserialize(NetDataReader reader)
    {
        Success = reader.GetBool();
        Message = reader.GetString();
        Character = reader.Get<CharacterDto>();
    }
}