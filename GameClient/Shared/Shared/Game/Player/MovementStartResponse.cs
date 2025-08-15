using Game.Core.Entities.Common.ValueObjetcs;
using LiteNetLib.Utils;

namespace Shared.Game.Player;

/// <summary>
/// Represents a state message sent from the server to the client.
/// This message contains the state of an entity, including its ID, position, and velocity.
/// It is used for state synchronization in a networked game environment.
/// </summary>
public struct MovementStartResponse : INetSerializable
{
    public int NetId { get; set; }
    public MapPosition TargetDirection { get; set; }
    public MapPosition CurrentPosition { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(TargetDirection.X);
        writer.Put(TargetDirection.Y);
        writer.Put(CurrentPosition.X);
        writer.Put(CurrentPosition.Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        TargetDirection = new MapPosition(reader.GetInt(), reader.GetInt());
        CurrentPosition = new MapPosition(reader.GetInt(), reader.GetInt());
    }
}