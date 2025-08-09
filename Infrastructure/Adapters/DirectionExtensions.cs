using Shared.Core.Enums;
using Shared.Infrastructure.Math;

namespace GameClient.Infrastructure.Adapters;

public static class DirectionExtensions
{
    /// <summary>
    /// Converte um vetor de direção (Vector2I) para uma direção enumerada (DirectionEnum).
    /// </summary>
    /// <param name="direction">Vetor de direção a ser convertido.</param>
    /// <returns>Direção enumerada correspondente.</returns>
    public static DirectionEnum VectorToDirection(this GridVector direction)
    {
        return direction switch
        {
            // Não cardinais
            { X: > 0, Y: > 0 } => DirectionEnum.SouthWest,
            { X: < 0, Y: > 0 } => DirectionEnum.SouthEast,
            { X: > 0, Y: < 0 } => DirectionEnum.NorthWest,
            { X: < 0, Y: < 0 } => DirectionEnum.NorthEast,
            _ => direction.X switch
            {
                > 0 => DirectionEnum.West,
                < 0 => DirectionEnum.East,
                _ => direction.Y switch
                {
                    > 0 => DirectionEnum.South,
                    < 0 => DirectionEnum.North,
                    _ => DirectionEnum.None
                }
            }
        };
    }

}