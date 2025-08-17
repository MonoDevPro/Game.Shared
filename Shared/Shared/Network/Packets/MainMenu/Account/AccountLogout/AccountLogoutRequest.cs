using LiteNetLib.Utils;

namespace Shared.Network.Packets.MainMenu.Account.AccountLogout;

public struct AccountLogoutRequest : INetSerializable
{
    public void Serialize(NetDataWriter writer)
    {
        // Nada a serializar
    }

    public void Deserialize(NetDataReader reader)
    {
        // Nada a desserializar
    }
}