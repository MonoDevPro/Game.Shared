using LiteNetLib.Utils;

namespace Shared.Infrastructure.Network.Data.Chat;

public struct ChatMessageBroadcast : INetSerializable
{
    public string SenderName;
    public string Message;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(SenderName);
        writer.Put(Message);
    }

    public void Deserialize(NetDataReader reader)
    {
        SenderName = reader.GetString();
        Message = reader.GetString();
    }
}