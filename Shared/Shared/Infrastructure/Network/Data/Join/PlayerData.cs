using LiteNetLib.Utils;
using Shared.Core.Enums;
using Shared.Infrastructure.Math;

namespace Shared.Infrastructure.Network.Data.Join;

public struct PlayerData : INetSerializable
{
    public PlayerData() { }
    
    public int NetId { get; set; } = 0; // Default Network ID
    public string Name { get; set; } = "Default Character";
    public string Description { get; set; } = "This is a default character description.";
    public VocationEnum Vocation = VocationEnum.None;
    public GenderEnum Gender = GenderEnum.None;
    public DirectionEnum Direction = DirectionEnum.South;
    public float Speed { get; set; } = 40.0f; // Default speed
    public GridVector GridPosition { get; set; } = GridVector.Zero; // Default position

    public void UpdateFromResource(ref PlayerData data)
    {
        NetId = data.NetId;
        Name = data.Name;
        Description = data.Description;
        Vocation = data.Vocation;
        Gender = data.Gender;
        Direction = data.Direction;
        Speed = data.Speed;
        GridPosition = data.GridPosition;
    }

    public override string ToString()
    {
        return $"PlayerResource(" +
               $"Id: {NetId}, " +
               $"Name: {Name}, " +
               $"Description: {Description}, " +
               $"Vocation: {Vocation}, " +
               $"Gender: {Gender}, " +
               $"Direction: {Direction}, " +
               $"Speed: {Speed}, " +
               $"GridPosition: {GridPosition})";
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(Name);
        writer.Put(Description);
        writer.Put((byte)Vocation);
        writer.Put((byte)Gender);
        writer.Put((byte)Direction);
        writer.Put(Speed);
        writer.Put(GridPosition.X);
        writer.Put(GridPosition.Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        NetId = reader.GetInt();
        Name = reader.GetString();
        Description = reader.GetString();
        Vocation = (VocationEnum)reader.GetByte();
        Gender = (GenderEnum)reader.GetByte();
        Direction = (DirectionEnum)reader.GetByte();
        Speed = reader.GetFloat();
        GridPosition = new GridVector(reader.GetInt(), reader.GetInt());
    }
}
