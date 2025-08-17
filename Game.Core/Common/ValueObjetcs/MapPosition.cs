using System.Runtime.CompilerServices;

namespace Game.Core.Common.ValueObjetcs;

/// <summary>
/// Representa uma posição ou direção no grid lógico do jogo.
/// </summary>
public readonly record struct MapPosition(int X, int Y)
{
    public static readonly MapPosition Zero = new(0, 0);
    public static readonly MapPosition North = new (0, -1);
    public static readonly MapPosition South = new (0, 1);
    public static readonly MapPosition East = new (1, 0);
    public static readonly MapPosition West = new (-1, 0);
    public static readonly MapPosition NorthWest = new (-1, -1);
    public static readonly MapPosition NorthEast = new (1, -1);
    public static readonly MapPosition SouthWest = new (-1, 1);
    public static readonly MapPosition SouthEast = new (1, 1);
    
    /// <summary>
    /// Retorna o quadrado da distância Euclidiana. Mais rápido para comparações.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int DistanceSquaredTo(MapPosition other)
    {
        int dx = other.X - X;
        int dy = other.Y - Y;
        return dx * dx + dy * dy;
    }
    
    /// <summary>
    /// Retorna a distância Euclidiana até outro ponto no grid.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DistanceTo(MapPosition other)
    {
        return (float)Math.Sqrt(DistanceSquaredTo(other));
    }
    
    public static MapPosition operator +(MapPosition a, MapPosition b) => new(a.X + b.X, a.Y + b.Y);
    public static MapPosition operator -(MapPosition a, MapPosition b) => new(a.X - b.X, a.Y - b.Y);
    public static MapPosition operator *(MapPosition a, int scalar) => new(a.X * scalar, a.Y * scalar);
    public static MapPosition operator /(MapPosition a, int scalar) => new(a.X / scalar, a.Y / scalar);
    
    public bool Equals(MapPosition other) => X == other.X && Y == other.Y;
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"Grid({X}, {Y})";
    
    public static MapPosition FromString(string s) 
    {
        var parts = s.Trim("Grid()".ToCharArray()).Split(',');
        if (parts.Length != 2) throw new FormatException("Invalid MapPosition format");
        return new MapPosition(int.Parse(parts[0]), int.Parse(parts[1]));
    }
}