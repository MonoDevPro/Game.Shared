using Game.Core.Entities.Common.Constants;
using Game.Core.Entities.Common.ValueObjetcs;

namespace GameClient.Core.Common;

public static class GridToWorld
{
    public static int TileSize => GameMapConstants.GridSize;

    public static WorldPosition ToWorld(MapPosition grid) => new WorldPosition(grid.X * TileSize, grid.Y * TileSize);
}
