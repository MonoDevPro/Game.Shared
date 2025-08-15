using System.Runtime.CompilerServices;

namespace Game.Core.Entities.Common.ValueObjetcs;

/// <summary>
/// Representa uma posição de píxeis no mundo visual do jogo.
/// </summary>
public readonly record struct WorldPosition(float X, float Y)
{
    // --- NOVOS MÉTODOS DE UTILIDADE ---

    /// <summary>
    /// Retorna o quadrado do comprimento do vetor. Mais rápido para comparações.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float LengthSquared() => X * X + Y * Y;

    /// <summary>
    /// Retorna o comprimento (magnitude) do vetor.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Length() => (float)System.Math.Sqrt(LengthSquared());

    /// <summary>
    /// Retorna a distância até outra posição no mundo.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DistanceTo(WorldPosition other)
    {
        float dx = other.X - X;
        float dy = other.Y - Y;
        return (float)System.Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Retorna o resultado da interpolação linear entre este vetor e outro.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public WorldPosition Lerp(WorldPosition to, float weight)
    {
        return new WorldPosition(
            X + (to.X - X) * weight,
            Y + (to.Y - Y) * weight
        );
    }
    
    public static readonly WorldPosition Zero = new(0, 0);
    public static readonly WorldPosition North = new (0.0f, -1f);
    public static readonly WorldPosition South = new (0.0f, 1f);
    public static readonly WorldPosition East = new (1f, 0.0f);
    public static readonly WorldPosition West = new (-1f, 0.0f);
    public static readonly WorldPosition NorthWest = new (-1f, -1f);
    public static readonly WorldPosition NorthEast = new (1f, -1f);
    public static readonly WorldPosition SouthWest = new (-1f, 1f);
    public static readonly WorldPosition SouthEast = new (1f, 1f);
    
    public static WorldPosition operator +(WorldPosition a, WorldPosition b) => new(a.X + b.X, a.Y + b.Y);
    public static WorldPosition operator -(WorldPosition a, WorldPosition b) => new(a.X - b.X, a.Y - b.Y);
    public static WorldPosition operator *(WorldPosition a, float scalar) => new(a.X * scalar, a.Y * scalar);
    public static WorldPosition operator /(WorldPosition a, float scalar) => new(a.X / scalar, a.Y / scalar);

    public bool Equals(WorldPosition other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"World({X}, {Y})";
}