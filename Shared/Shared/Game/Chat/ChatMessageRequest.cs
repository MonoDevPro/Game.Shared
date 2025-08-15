using LiteNetLib.Utils;

namespace Shared.Features.Game.Chat.Packets;

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