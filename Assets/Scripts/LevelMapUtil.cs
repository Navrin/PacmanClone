using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelMapUtil
{

    static LevelMapUtil Instance;

    private BoundsInt cellBounds;
    private Vector2Int min => (Vector2Int)cellBounds.min;
    
    private Vector2Int center = new Vector2Int(0, 0);

    private int[,] levelMap;
    
    public static LevelMapUtil GetInstance()
    {
        if (Instance == null)
        {
            LevelMapUtil.Instance = new LevelMapUtil();
        }

        return Instance;
    }

    public void ParseMap(Tilemap tilemap, Tileset tileset)
    {
        cellBounds = tilemap.cellBounds;
        levelMap = new int[cellBounds.size.x, cellBounds.size.y];
        
        for (var xi =cellBounds.xMin; xi <cellBounds.size.x; xi++)
        for (var yi = cellBounds.yMin; yi < cellBounds.size.y; yi++)
        {
            var x = xi + cellBounds.xMin;
            var y = yi + cellBounds.yMin;
            levelMap[x, y] = tileset.TileToMap(tilemap.GetTile(new Vector3Int(x, y, 0)));
        }
    }

    public int Get(int x, int y)
    {
        if (levelMap == null) throw new Exception("LevelMapUtil.Get was called but level map is null");
        var point = new Vector2Int(x, y);
        point += min;
        
        return levelMap[point.x, point.y];
    }
}