using LiteNetLib.Utils;
using Shared.Infrastructure.Math;

namespace Shared.Core.Network.Data.Input;

/// <summary>
/// Represents an input message sent from the client to the server.
/// </summary>
public struct MovementRequest : INetSerializable
{
    public uint SequenceId;
    public GridVector Direction; // Alterado para Vector2I
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(SequenceId);
        writer.Put(Direction.X);
        writer.Put(Direction.Y);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        SequenceId = reader.GetUInt();
        Direction = new GridVector(reader.GetInt(), reader.GetInt());
    }
}