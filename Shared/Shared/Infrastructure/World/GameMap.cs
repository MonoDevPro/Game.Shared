using Shared.Infrastructure.Math;

namespace Shared.Infrastructure.World;

public class GameMap
{
    private readonly Tile[,] _tiles;
    public int Width => _tiles.GetLength(0);
    public int Height => _tiles.GetLength(1);

    public GameMap(int width, int height)
    {
        _tiles = new Tile[width, height];
    }

    public void SetTile(int x, int y, Tile tile)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            _tiles[x, y] = tile;
    }

    public bool IsTileWalkable(GridVector position)
    {
        if (position.X < 0 || position.X >= Width || position.Y < 0 || position.Y >= Height)
            return false; // Fora dos limites

        return _tiles[position.X, position.Y].IsWalkable;
    }
}