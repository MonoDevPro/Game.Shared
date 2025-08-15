using Game.Core.Entities.Character;
using Game.Core.Entities.Common.Enums;
using Game.Core.Entities.Common.ValueObjetcs;
using LiteNetLib.Utils;

namespace Shared.Helpers;

public static class SerializationExtensions
{
    public static void Put(this NetDataWriter writer, MapPosition position)
    {
        writer.Put(position.X);
        writer.Put(position.Y);
    }
    public static MapPosition DeserializeMapPosition(this NetDataReader reader)
    {
        int x = reader.GetInt();
        int y = reader.GetInt();
        return new MapPosition(x, y);
    }
    
    public static void Put(this NetDataWriter writer, CharacterDto character)
    {
        writer.Put(character.Id);
        writer.Put(character.Name);
        writer.Put((byte)character.Vocation);
        writer.Put((byte)character.Gender);
        writer.Put((byte)character.Direction);
        writer.Put(character.Position);
        writer.Put(character.Speed);
    }
    public static CharacterDto DeserializeCharacterDto(this NetDataReader reader)
    {
        var character = new CharacterDto
        {
            Id = reader.GetInt(),
            Name = reader.GetString(),
            Vocation = (VocationEnum)reader.GetByte(),
            Gender = (GenderEnum)reader.GetByte(),
            Direction = (DirectionEnum)reader.GetByte(),
            Position = reader.DeserializeMapPosition(),
            Speed = reader.GetFloat()
        };
        return character;
    }
}