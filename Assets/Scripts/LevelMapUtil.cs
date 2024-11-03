using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;


// referenced: https://erdiizgi.com/data-structure-for-games-priority-queue-for-unity-in-c/

public class LevelMapUtil
{
    public BoundsInt GhostSpawnArea;

    private LevelMapUtil()
    {
        GhostSpawnArea.SetMinMax(new Vector3Int(-4, 2,0), new Vector3Int(3, -2, 10000));
    }
    static LevelMapUtil _instance;

    public BoundsInt CellBounds;
    private Tilemap _tilemapRef;
    
    // private Vector2Int Min => (Vector2Int)cellBounds.min;
    
    private int[,] _levelMap;
    
    public static LevelMapUtil GetInstance()
    {
        if (_instance == null)
        {
            LevelMapUtil._instance = new LevelMapUtil();
        }

        return _instance;
    }

    public Vector3Int GetPosFromTransform(Transform transform)
    {
        return _tilemapRef.WorldToCell(transform.position);
    }
    public void ParseMap(Tilemap tilemap, Tileset tileset)
    {
        _tilemapRef = tilemap;
        CellBounds = tilemap.cellBounds;
        _levelMap = new int[CellBounds.size.y, CellBounds.size.x];
        
        for (var xi =0; xi <CellBounds.size.x; xi++)
        for (var yi = 0; yi < CellBounds.size.y; yi++)
        {
            var x = xi + CellBounds.xMin;
            var y = yi + CellBounds.yMin;
            _levelMap[yi, xi] = tileset.TileToMap(tilemap.GetTile(new Vector3Int(x, y, 0)));
            
        }

    }

    public int Get(int x, int y)
    {
        if (_levelMap == null) throw new Exception("LevelMapUtil.Get was called but level map is null");
        var point = TranslatePoint(x, y);

        return _levelMap[point.x, point.y];
    }

    private Vector2Int TranslatePoint(int x, int y)
    {
        var point = new Vector2Int(x + Math.Abs(CellBounds.xMin), y + Math.Abs(CellBounds.yMin));
        return point;
    }

    private int? GetChecked(int x, int y)
    {
        var point = TranslatePoint(x, y);
        if (CellBounds.Contains(new Vector3Int(x, y, 0)))
        {
            return _levelMap[point.y, point.x];
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
            
        
            if (CheckMask(SolidWallMask, tile.Value)) continue;
            outFlag |= (DirectionFlag)dir;
        } 
        return outFlag;
    }

    private List<Vector2Int> GetValidNeighbourTiles(Vector2Int pos, Direction? preventBackstep, bool blockSpawnArea = true)
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
    private const int SolidWallMask =
        (1 << TileType.OutsideCorner
        | 1 << TileType.OutsideWall
        | 1 << TileType.InsideCorner
        | 1 << TileType.InsideWall);

    // private const int pickupMask =
    //     (1 << TileType.Pellet
    //      | 1 << TileType.PowerUp);
    private static bool CheckMask(int mask, int tile)
    {
        return (mask & (1 << tile)) > 0;
    }

    private static void Path(Dictionary<Vector2Int, Vector2Int> paths, Vector2Int start, ref Stack<Vector2Int> points)
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
        heuristic ??= Vector2Int.Distance;
        
        // var openSet = new WeightedQueue<Vector2Int, float>(0);
        // openSet.Insert(start, heuristic(start, start));
        var openSet = new List<Vector2Int> { start };

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