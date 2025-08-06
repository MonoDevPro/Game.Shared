using LiteNetLib.Utils;

namespace Game.Shared.Shared.Infrastructure.Network.Data.Chat;

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