using LiteNetLib.Utils;

namespace Shared.MainMenu.Account.AccountLogin;

public struct AccountLoginRequest : INetSerializable
{
    public string Username;
    public string Password;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Username);
        writer.Put(Password);
    }

    public void Deserialize(NetDataReader reader)
    {
        Username = reader.GetString();
        Password = reader.GetString();
    }
}