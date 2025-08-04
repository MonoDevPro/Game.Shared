using Godot;
using LiteNetLib.Utils;

namespace Game.Shared.Shared.Infrastructure.Network.Data.Input;

/// <summary>
/// Represents an input message sent from the client to the server.
/// </summary>
public struct MovementRequest : INetSerializable
{
    public Vector2I Direction; // Alterado para Vector2I
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Direction.X);
        writer.Put(Direction.Y);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        Direction = new Vector2I(reader.GetInt(), reader.GetInt());
    }
}