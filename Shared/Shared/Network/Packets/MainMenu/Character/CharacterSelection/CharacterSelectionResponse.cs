using LiteNetLib.Utils;

namespace Shared.Network.Packets.MainMenu.Character.CharacterSelection;

public struct CharacterSelectionResponse : INetSerializable
{
    public bool Success;
    public string Message;
    public CharacterData Character;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Success);
        writer.Put(Message);
        writer.Put<CharacterData>(Character);
    }

    public void Deserialize(NetDataReader reader)
    {
        Success = reader.GetBool();
        Message = reader.GetString();
        Character = reader.Get<CharacterData>();
    }
}