using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;


// referenced: https://erdiizgi.com/data-structure-for-games-priority-queue-for-unity-in-c/

public class LevelMapUtil
{
    public BoundsInt GhostSpawnArea = new BoundsInt();

    private LevelMapUtil()
    {
        GhostSpawnArea.SetMinMax(new Vector3Int(-4, 2,0), new Vector3Int(3, -2, 10000));
    }
    static LevelMapUtil Instance;

    public BoundsInt cellBounds;
    private Tilemap _tilemapRef;
    
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

    public Vector3Int GetPosFromTransform(Transform transform)
    {
        return _tilemapRef.WorldToCell(transform.position);
    }
    public void ParseMap(Tilemap tilemap, Tileset tileset)
    {
        _tilemapRef = tilemap;
        cellBounds = tilemap.cellBounds;
        levelMap = new int[cellBounds.size.y, cellBounds.size.x];
        
        for (var xi =0; xi <cellBounds.size.x; xi++)
        for (var yi = 0; yi < cellBounds.size.y; yi++)
        {
            var x = xi + cellBounds.xMin;
            var y = yi + cellBounds.yMin;
            levelMap[yi, xi] = tileset.TileToMap(tilemap.GetTile(new Vector3Int(x, y, 0)));
            
        }

    }

    public int Get(int x, int y)
    {
        if (levelMap == null) throw new Exception("LevelMapUtil.Get was called but level map is null");
        var point = TranslatePoint(x, y);

        return levelMap[point.x, point.y];
    }

    private Vector2Int TranslatePoint(int x, int y)
    {
        var point = new Vector2Int(x + Math.Abs(cellBounds.xMin), y + Math.Abs(cellBounds.yMin));
        return point;
    }

    public int? GetChecked(int x, int y)
    {
        var point = TranslatePoint(x, y);
        if (cellBounds.Contains((Vector3Int)new Vector3Int(x, y, 0)))
        {
            return levelMap[point.y, point.x];
        }
        return null;
    }

    /**
     */
    public DirectionFlag GetValidMovements(int x, int y, bool blockSpawnArea = true)
    {
        DirectionFlag outFlag = DirectionFlag.None;
        
        foreach (Direction dir in DirectionMethods.AllDirections)
        {
            var vec = dir.ToIntVec();
            var nextPos = new Vector2Int(x + vec.x, y + vec.y);
            if (blockSpawnArea && GhostSpawnArea.Contains((Vector3Int)nextPos))
            {
                continue;
            }
            
            var tile = GetChecked(nextPos.x, nextPos.y);
            
            if (!tile.HasValue) continue;
            
        
            if (CheckMask(solidWallMask, tile.Value)) continue;
            outFlag |= (DirectionFlag)dir;
        } 
        return outFlag;
    }

    public List<Vector2Int> GetValidNeighbourTiles(Vector2Int pos, Direction? preventBackstep, bool blockSpawnArea = true)
    {
        var flag = GetValidMovements(pos.x, pos.y, blockSpawnArea);
        if (preventBackstep.HasValue)
        {
            flag ^= (DirectionFlag)preventBackstep.Value;
        }

        return (
            from Direction dir in DirectionMethods.AllDirections 
            let valid = ((short)flag & (short)dir) > 0 
            where valid select pos + (Vector2Int)dir.ToIntVec()
        ).ToList();
    }
    
    /**
     * TODO: remove these from the pac controller and centralise behaviour
     */
    private readonly int solidWallMask =
        (1 << TileType.OutsideCorner
        | 1 << TileType.OutsideWall
        | 1 << TileType.InsideCorner
        | 1 << TileType.InsideWall);

    private readonly int pickupMask =
        (1 << TileType.Pellet
         | 1 << TileType.PowerUp);
    private static bool CheckMask(int mask, int tile)
    {
        return (mask & (1 << tile)) > 0;
    }

    protected static void Path(Dictionary<Vector2Int, Vector2Int> paths, Vector2Int start, ref Stack<Vector2Int> points)
    {
        points.Push(start);
        var current = start; 
        var pathKeys = paths.Keys.ToList();
        while (pathKeys.Contains(current))
        {
            current = paths[current];
            points.Push(current);
        }
    }
    public bool NavigateTowardsGoal(
        Vector2Int start, 
        Vector2Int goal, 
        ref Stack<Vector2Int> points,
        Direction? preventBackstep = null,
        Func<Vector2Int, Vector2Int, float> heuristic = null,
        bool blockSpawnArea = true)
    {
        if (heuristic == null) heuristic = Vector2Int.Distance;
        
        // var openSet = new WeightedQueue<Vector2Int, float>(0);
        // openSet.Insert(start, heuristic(start, start));
        var openSet = new List<Vector2Int>();
        openSet.Add(start);
        
        var fscore = new Dictionary<Vector2Int, float> { { start, 0 } };

        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float> { { start, heuristic(start, start) } };

        while (openSet.Count > 0)
        {
            // var c = openSet.Pop();
            var c = openSet
                .Aggregate((a,b) => 
                    fscore.GetValueOrDefault(a, float.PositiveInfinity) < fscore.GetValueOrDefault(b, float.PositiveInfinity) ? a : b);
            
            if (c == goal)
            {
                Path(cameFrom, c, ref points);
                return true;
            }

            List<Vector2Int> neighbours;
            
            if (cameFrom.ContainsKey(c) && cameFrom[c] == start)
            {
                neighbours = GetValidNeighbourTiles(c, preventBackstep, blockSpawnArea);
            }
            else
            {
                neighbours = GetValidNeighbourTiles(c, null, blockSpawnArea);
            }
            openSet.Remove(c);
            foreach (var n in neighbours)
            {
                var score = gScore.GetValueOrDefault(c, float.PositiveInfinity) + heuristic(c, n);
                if (score < gScore.GetValueOrDefault(n, float.PositiveInfinity))
                {
                    cameFrom.Add(n, c);
                    gScore.Add(n, score);
                    fscore.Add(n, score + heuristic(n, goal));
                    if (!openSet.Contains(n))
                        openSet.Add(n);
                }
            }

        }

        return false;
    }
}