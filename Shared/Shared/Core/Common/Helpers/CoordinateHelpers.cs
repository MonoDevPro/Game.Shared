using Shared.Core.Common.Constants;
using Shared.Infrastructure.Math;

namespace Shared.Core.Common.Helpers;

public static class CoordinateHelpers
{
    // O método de extensão para GridVector
    public static WorldPosition ToWorldPosition(this GridVector gridVector)
    {
        return new WorldPosition(gridVector.X * GameMapConstants.GridSize, gridVector.Y * GameMapConstants.GridSize);
    }
    
    public static GridVector ToGridVector(this WorldPosition worldPosition)
    {
        return new GridVector(
            (int)(worldPosition.X / GameMapConstants.GridSize),
            (int)(worldPosition.Y / GameMapConstants.GridSize)
        );
    }
}