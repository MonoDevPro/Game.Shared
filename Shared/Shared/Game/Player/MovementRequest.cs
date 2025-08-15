using Game.Core.Entities.Common.ValueObjetcs;
using LiteNetLib.Utils;

namespace Shared.Game.Player;

/// <summary>
/// Represents an input message sent from the client to the server.
/// </summary>
public struct MovementRequest : INetSerializable
{
    public uint SequenceId;
    public MapPosition Direction; // Alterado para Vector2I
    
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(SequenceId);
        writer.Put(Direction.X);
        writer.Put(Direction.Y);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        SequenceId = reader.GetUInt();
        Direction = new MapPosition(reader.GetInt(), reader.GetInt());
    }
}