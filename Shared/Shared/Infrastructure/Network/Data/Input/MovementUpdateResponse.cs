using LiteNetLib.Utils;
using Shared.Core.Enums;
using Shared.Infrastructure.Math;

namespace Shared.Infrastructure.Network.Data.Input;

/// <summary>
/// Represents a state message sent from the server to the client.
/// This message contains the state of an entity, including its ID, position, and velocity.
/// It is used for state synchronization in a networked game environment.
/// </summary>
public struct MovementUpdateResponse : INetSerializable
{
    public int NetId { get; set; }
    public GridVector DirectionInput { get; set; }
    public GridVector LastGridPosition { get; set; }
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(LastGridPosition.X);
        writer.Put(LastGridPosition.Y);
        writer.Put(DirectionInput.X);
        writer.Put(DirectionInput.Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        DirectionInput = new GridVector(reader.GetInt(), reader.GetInt());
        LastGridPosition = new GridVector(reader.GetInt(), reader.GetInt());
    }
}