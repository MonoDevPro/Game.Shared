using Game.Core.Entities.Character;
using Game.Core.Entities.Common.Enums;
using Game.Core.Entities.Common.ValueObjetcs;
using LiteNetLib.Utils;
using Shared.Helpers;

namespace Shared.Game.Player;

public struct PlayerData : INetSerializable
{
    public PlayerData() { }
    public int NetId { get; set; } = 0; // Default Network ID
    public string Name { get; set; } = "DefaultPlayer";
    public VocationEnum Vocation { get; set; } = VocationEnum.None;
    public GenderEnum Gender { get; set; } = GenderEnum.None;
    public DirectionEnum Direction { get; set; } = DirectionEnum.South;
    public float Speed { get; set; } = 1.0f; // Default speed
    public MapPosition GridPosition { get; set; }
    public string Description { get; set; } = "This is a default character description.";

    public override string ToString()
    {
        return $"PlayerResource(" +
               $"Id: {NetId}, " +
               $"Name: {Name}, " +
               $"Vocation: {Vocation}, " +
               $"Gender: {Gender}, " +
               $"Direction: {Direction}, " +
               $"Speed: {Speed}, " +
               $"MapPosition: {GridPosition}), " +
               $"Description: {Description})";
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(NetId);
        writer.Put(Name);
        writer.Put((byte)Vocation);
        writer.Put((byte)Gender);
        writer.Put((byte)Direction);
        writer.Put(Speed);
        writer.Put(GridPosition);
        writer.Put(Description);
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
        GridPosition = reader.DeserializeMapPosition();
    }
}
