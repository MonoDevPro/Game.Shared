using Game.Core.Common.Constants;
using Game.Core.Common.ValueObjetcs;

namespace Game.Core.Common.Helpers;

public static class CoordinateHelpers
{
    // O método de extensão para GridVector
    public static WorldPosition ToWorldPosition(this MapPosition gridVector)
    {
        return new WorldPosition(gridVector.X * GameMapConstants.GridSize, gridVector.Y * GameMapConstants.GridSize);
    }
    
    public static MapPosition ToMapPosition(this WorldPosition worldPosition)
    {
        return new MapPosition(
            (int)(worldPosition.X / GameMapConstants.GridSize),
            (int)(worldPosition.Y / GameMapConstants.GridSize)
        );
    }
}