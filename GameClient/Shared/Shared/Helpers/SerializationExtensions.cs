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
    // CharacterDto removed; keep only MapPosition helpers. If needed, use CharacterData serializers in Shared/MainMenu.
}