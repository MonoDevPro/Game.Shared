using Godot;
using Shared.Infrastructure.Math;

namespace GameClient.Core.Common;

/// <summary>
/// Converte tipos de dados do domínio do jogo para tipos da engine Godot e vice-versa.
/// </summary>
public static class GodotAdapter
{
    // De Godot para o Domínio
    public static GridVector ToGridVector(this Vector2I godotVector)
    {
        return new GridVector(godotVector.X, godotVector.Y);
    }

    // Do Domínio para Godot
    public static Vector2 ToGodotVector2(this WorldPosition worldPosition)
    {
        return new Vector2(worldPosition.X, worldPosition.Y);
    }

    public static Vector2I ToGodotVector2I(this GridVector gridVector)
    {
        return new Vector2I(gridVector.X, gridVector.Y);
    }
}