using LiteNetLib.Utils;

namespace Shared.Infrastructure.Math;

/// <summary>
/// Representa uma posição de píxeis no mundo visual do jogo.
/// </summary>
public record struct WorldPosition(float X, float Y) : INetSerializable
{
    public static readonly WorldPosition Zero = new(0, 0);
    public static WorldPosition operator +(WorldPosition a, WorldPosition b) => new(a.X + b.X, a.Y + b.Y);
    public static WorldPosition operator -(WorldPosition a, WorldPosition b) => new(a.X - b.X, a.Y - b.Y);
    public static WorldPosition operator *(WorldPosition a, float scalar) => new(a.X * scalar, a.Y * scalar);
    public static WorldPosition operator /(WorldPosition a, float scalar) => new(a.X / scalar, a.Y / scalar);
    
    public static WorldPosition FromGridPosition(GridVector gridPos, float cellSize)
    {
        return new WorldPosition(
            gridPos.X * cellSize + cellSize / 2,
            gridPos.Y * cellSize + cellSize / 2
        );
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(X);
        writer.Put(Y);
    }

    public void Deserialize(NetDataReader reader)
    {
        X = reader.GetFloat();
        Y = reader.GetFloat();
    }
    
    public bool Equals(WorldPosition other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"World({X}, {Y})";
}