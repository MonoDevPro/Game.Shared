using LiteNetLib.Utils;

namespace Shared.Features.MainMenu.Account;

public struct AccountDto : INetSerializable
{
    public int AccountId;
    public string Username;
    public string Email;
    public string Password;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(AccountId);
        writer.Put(Username);
        writer.Put(Email);
        writer.Put(Password);
    }

    public void Deserialize(NetDataReader reader)
    {
        AccountId = reader.GetInt();
        Username = reader.GetString();
        Email = reader.GetString();
        Password = reader.GetString();
    }
}