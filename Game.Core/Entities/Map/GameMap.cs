using Game.Core.Entities.Common.ValueObjetcs;

namespace Game.Core.Entities.Map;

public class GameMap
{
    private readonly Tile[,] _tiles;
    public int Width => _tiles.GetLength(0);
    public int Height => _tiles.GetLength(1);

    public GameMap(int width, int height)
    {
        _tiles = new Tile[width, height];
        
        // Inicializa todas as tiles como caminháveis por padrão
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                _tiles[x, y] = new Tile { IsWalkable = true };
    }

    public void SetTile(int x, int y, Tile tile)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            _tiles[x, y] = tile;
    }

    public bool IsTileWalkable(MapPosition position)
    {
        if (position.X < 0 || position.X >= Width || position.Y < 0 || position.Y >= Height)
            return false; // Fora dos limites

        return _tiles[position.X, position.Y].IsWalkable;
    }
}