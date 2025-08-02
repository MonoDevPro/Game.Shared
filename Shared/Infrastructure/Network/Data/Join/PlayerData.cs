using Game.Shared.Shared.Enums;
using Godot;
using LiteNetLib.Utils;

namespace Game.Shared.Shared.Infrastructure.Network.Data.Join;

public struct PlayerData : INetSerializable
{
    public PlayerData() { }
    
    public int NetId { get; set; } = 0; // Default Network ID
    public string Name { get; set; } = "Default Character";
    public string Description { get; set; } = "This is a default character description.";
    public VocationEnum Vocation = VocationEnum.None;
    public GenderEnum Gender = GenderEnum.None;
    public Vector2I GridPosition { get; set; } = Vector2I.Zero; // Default position

    public void UpdateFromResource(ref PlayerData data)
    {
        NetId = data.NetId;
        Name = data.Name;
        Description = data.Description;
        Vocation = data.Vocation;
        Gender = data.Gender;
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
               $"GridPosition: {GridPosition})";
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(Name);
        writer.Put(Description);
        writer.Put((byte)Vocation);
        writer.Put((byte)Gender);
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
        GridPosition = new Vector2I(reader.GetInt(), reader.GetInt());
    }
}
