using Game.Shared.Scripts.Shared.Network.Seralization.Extensions;
using Godot;
using LiteNetLib.Utils;

namespace Game.Shared.Scripts.Shared.Network.Data.Input;

/// <summary>
/// Represents a state message sent from the server to the client.
/// This message contains the state of an entity, including its ID, position, and velocity.
/// It is used for state synchronization in a networked game environment.
/// </summary>
public struct StateResponse : INetSerializable
{
    public int NetId { get; set; }
    public Vector2I GridPosition; // Alterado para Vector2I
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(GridPosition.X);
        writer.Put(GridPosition.Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        GridPosition = new Vector2I(reader.GetInt(), reader.GetInt());
    }
}