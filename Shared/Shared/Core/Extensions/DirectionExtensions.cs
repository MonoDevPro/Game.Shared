using Shared.Core.Enums;
using Shared.Infrastructure.Math;

namespace Shared.Core.Extensions;

public static class DirectionExtensions
{
    /// <summary>
    /// Converte um vetor de direção (Vector2I) para uma direção enumerada (DirectionEnum).
    /// </summary>
    /// <param name="direction">Vetor de direção a ser convertido.</param>
    /// <returns>Direção enumerada correspondente.</returns>
    /// <summary>
    /// Converte um vetor de direção (GridVector) para uma direção enumerada (DirectionEnum).
    /// </summary>
    public static DirectionEnum VectorToDirection(this GridVector direction)
    {
        int x = direction.X;
        int y = direction.Y;

        // Verifica os movimentos para cima (Norte, Noroeste, Nordeste)
        if (y < 0)
        {
            if (x < 0) return DirectionEnum.NorthWest;
            if (x > 0) return DirectionEnum.NorthEast;
            return DirectionEnum.North;
        }

        // Verifica os movimentos para baixo (Sul, Sudoeste, Sudeste)
        if (y > 0)
        {
            if (x < 0) return DirectionEnum.SouthWest;
            if (x > 0) return DirectionEnum.SouthEast;
            return DirectionEnum.South;
        }

        // Se Y é 0, só pode ser Leste ou Oeste
        if (x < 0) return DirectionEnum.West;
        if (x > 0) return DirectionEnum.East;
        
        // Se X e Y são 0, não há direção
        return DirectionEnum.None;
    }

    /// <summary>
    /// Converte uma direção enumerada (DirectionEnum) para um vetor de direção (GridVector).
    /// </summary>
    public static GridVector DirectionToVector(this DirectionEnum direction)
    {
        return direction switch
        {
            DirectionEnum.North => new GridVector(0, -1),
            DirectionEnum.NorthEast => new GridVector(1, -1),
            DirectionEnum.East => new GridVector(1, 0),
            DirectionEnum.SouthEast => new GridVector(1, 1),
            DirectionEnum.South => new GridVector(0, 1),
            DirectionEnum.SouthWest => new GridVector(-1, 1),
            DirectionEnum.West => new GridVector(-1, 0),
            DirectionEnum.NorthWest => new GridVector(-1, -1),
            _ => new GridVector(0, 0),
        };
    }

}