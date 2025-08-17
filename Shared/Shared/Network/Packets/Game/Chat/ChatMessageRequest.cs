using LiteNetLib.Utils;

namespace Shared.Network.Packets.Game.Chat;

public struct ChatMessageRequest : INetSerializable
{
    public string Message;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Message);
    }

    public void Deserialize(NetDataReader reader)
    {
        Message = reader.GetString();
    }
}