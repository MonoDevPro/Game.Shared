using LiteNetLib.Utils;

namespace Shared.MainMenu.Account.AccountLogin;

public struct AccountLoginResponse : INetSerializable
{
    public bool Success;
    public string Message;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Success);
        writer.Put(Message);
    }

    public void Deserialize(NetDataReader reader)
    {
        Success = reader.GetBool();
        Message = reader.GetString();
    }
}