using System.Runtime.CompilerServices;
using LiteNetLib.Utils;

namespace Shared.Infrastructure.Math;

/// <summary>
/// Representa uma posição ou direção no grid lógico do jogo.
/// </summary>
public record struct GridVector(int X, int Y) : INetSerializable
{
    public static readonly GridVector Zero = new(0, 0);
    public static readonly GridVector North = new (0, -1);
    public static readonly GridVector South = new (0, 1);
    public static readonly GridVector East = new (1, 0);
    public static readonly GridVector West = new (-1, 0);
    public static readonly GridVector NorthWest = new (-1, -1);
    public static readonly GridVector NorthEast = new (1, -1);
    public static readonly GridVector SouthWest = new (-1, 1);
    public static readonly GridVector SouthEast = new (1, 1);
    
    // --- NOVOS MÉTODOS DE UTILIDADE ---

    /// <summary>
    /// Retorna o quadrado da distância Euclidiana. Mais rápido para comparações.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int DistanceSquaredTo(GridVector other)
    {
        int dx = other.X - X;
        int dy = other.Y - Y;
        return dx * dx + dy * dy;
    }
    
    /// <summary>
    /// Retorna a distância Euclidiana até outro ponto no grid.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DistanceTo(GridVector other)
    {
        return (float)System.Math.Sqrt(DistanceSquaredTo(other));
    }
    
    public static GridVector operator +(GridVector a, GridVector b) => new(a.X + b.X, a.Y + b.Y);
    public static GridVector operator -(GridVector a, GridVector b) => new(a.X - b.X, a.Y - b.Y);
    public static GridVector operator *(GridVector a, int scalar) => new(a.X * scalar, a.Y * scalar);
    public static GridVector operator /(GridVector a, int scalar) => new(a.X / scalar, a.Y / scalar);
    
    public static GridVector FromWorldPosition(WorldPosition position, float cellSize)
    {
        return new GridVector(
            (int)(position.X / cellSize),
            (int)(position.Y / cellSize)
        );
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(X);
        writer.Put(Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        X = reader.GetInt();
        Y = reader.GetInt();
    }

    public bool Equals(GridVector other) => X == other.X && Y == other.Y;
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"Grid({X}, {Y})";
}