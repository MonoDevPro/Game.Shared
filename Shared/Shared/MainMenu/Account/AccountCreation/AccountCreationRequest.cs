using LiteNetLib.Utils;

namespace Shared.MainMenu.Account.AccountCreation;

public struct AccountCreationRequest : INetSerializable
{
    public string Username;
    public string Email;
    public string Password;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Username);
        writer.Put(Email);
        writer.Put(Password);
    }

    public void Deserialize(NetDataReader reader)
    {
        Username = reader.GetString();
        Email = reader.GetString();
        Password = reader.GetString();
    }
}