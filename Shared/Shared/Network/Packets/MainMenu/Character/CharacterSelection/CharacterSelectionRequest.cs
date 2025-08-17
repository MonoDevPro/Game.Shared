using LiteNetLib.Utils;

namespace Shared.Network.Packets.MainMenu.Character.CharacterSelection;

public struct CharacterSelectionRequest : INetSerializable
{
    public int CharacterId;
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CharacterId);
    }
    public void Deserialize(NetDataReader reader)
    {
        CharacterId = reader.GetInt();
    }
}