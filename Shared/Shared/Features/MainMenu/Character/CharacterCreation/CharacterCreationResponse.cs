using LiteNetLib.Utils;

namespace Shared.Features.MainMenu.Character.CharacterCreation;

public struct CharacterCreationResponse : INetSerializable
{
    public bool Success;
    public string Message;
    public CharacterDataModel Character;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Success);
        writer.Put(Message);
        writer.Put(Character);
    }

    public void Deserialize(NetDataReader reader)
    {
        Success = reader.GetBool();
        Message = reader.GetString();
    }
}