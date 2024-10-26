using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;
using System.Runtime.CompilerServices; 
internal static class TileType
{
    public const int Empty = 0;
    public const int OutsideCorner = 1;
    public const int OutsideWall = 2;
    public const int InsideCorner = 3;
    public const int InsideWall = 4;
    public const int Pellet = 5;
    public const int PowerUp = 6;
    public const int Junction = 7;
}

/**
 * TODO: change this to be a scriptable object (i forgot the term)
 */
[Serializable]
public class Tileset
{
    public TileBase innerWall;
    public TileBase innerCorner;
    public TileBase outerWall;
    public TileBase outerCorner;
    public TileBase junction;
    public TileBase pellet;
    public TileBase powerUp;

    public int TileToMap(TileBase tile)
    {
        if (tile == innerWall) return TileType.InsideWall;
        if (tile == innerCorner) return TileType.InsideCorner;
        if (tile == outerWall) return TileType.OutsideCorner;
        if (tile == outerCorner) return TileType.OutsideCorner;
        if (tile == junction) return TileType.Junction;
        if (tile == pellet) return TileType.Pellet;
        if (tile == powerUp) return TileType.PowerUp;
        return -1;
    }
}

[ExecuteAlways]
public class LevelGenerator : MonoBehaviour
{
    readonly int[,] _levelMap =
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,0},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0},
    };

    internal int[,] FullLevelMap { get; private set; }

    void Start()
    {
        FullLevelMap = new int[_levelMap.GetLength(1)*2, _levelMap.GetLength(0)*2];
        FixAllTileRotations();
    }

    private int[,] _transLevelMap;

    int[,] levelMap
    {
        get { return _transLevelMap ??= _levelMap; }
        set => _transLevelMap = value;
    }

    public Tilemap tilemap;
    // ok... so no rule tiles because "challenge" i guess
    // I guess we're reinventing rule tiles!
    // public RuleTile smartTile;
    public Tileset tileset;
    
    public bool generateMap = true;

    public int pelletCount = -1;
    public int powerupCount = -1;
    
    protected TileBase getTileMapping(int kind)
    {
        return kind switch
        {
            1 => tileset.outerCorner,
            2 => tileset.outerWall,
            3 => tileset.innerCorner,
            4 => tileset.innerWall,
            5 => tileset.pellet,
            6 => tileset.powerUp,
            7 => tileset.junction,
            _ => throw new System.ArgumentException($"Invalid tilemap kind {kind}")
        }
        ;
    }
    void GenerateLevelCorner(bool mirrorX, bool mirrorY)
    {
        // TODO: make this more elegant
        levelMap = _levelMap;
        if (mirrorY) levelMap = FlipMapY();
        if (mirrorX) levelMap = FlipMapX(); 
        // Debug.Log(PrettyFormatMatrix(levelMap));
        // var tileable = new int[] { 1, 2, 3, 4, 7 };
        var w = levelMap.GetLength(0);
        var h = levelMap.GetLength(1);
        

        for (var x = 0; x < w; x++)
        {
            for (var y = 0; y < h; y++)
            {
                var transposedCoords = new Vector3Int(
                    (mirrorX ? w : 1) + (-w + y) /* * (mirrorX ? -1 : 1) */,
                    (mirrorY ? -h  :- 0) + (-x + h) /* * (mirrorY ? -1 : 1) */,  0);
                
                if (levelMap[x,y] != 0)
                {
                    // tilemap.SetTile(transposedCoords, smartTile);
                    var tile = getTileMapping(levelMap[x, y]);
                    
                    tilemap.SetTile(transposedCoords, tile);
                }

            }
        }
        
    }

    Quaternion CombineRotations(Quaternion q1, Quaternion q2)
    {
        var q1Euler = q1.eulerAngles;
        var q2Euler = q2.eulerAngles;
        return Quaternion.Euler(
            q1Euler.x + q2Euler.x,
            q1Euler.y + q2Euler.y,
            q1Euler.z + q2Euler.z
        );
    }

    void FixAllTileRotations()
    {
        var bounds = (tilemap.cellBounds);
        // FullLevelMap = new int[bounds.size.x, bounds.size.y];
        pelletCount = 0;
        powerupCount = 0;
       
        for (var x = bounds.position.x; x < bounds.size.x; x++)
        for (var y=bounds.position.y; y < bounds.size.y; y++)
        {
            var pos = new Vector3Int(x,y,0);
            var rotation =     CalculateRotationForTile(x, y);
            var tile = tilemap.GetTile(pos);
            if (tile == tileset.pellet) pelletCount++;
            if (tile == tileset.powerUp) powerupCount++;
            // try
            // {
            //     FullLevelMap[x, y] = tileset.TileToMap(tile);
            // }
            // catch (System.IndexOutOfRangeException e)
            // {
            //     Debug.Log($"OOB: {x}, {y}");
            // }
            // var origTrans = tilemap.GetTransformMatrix(pos);
            tilemap.SetTransformMatrix(pos, 
                Matrix4x4.TRS(
                    // origTrans.GetPosition(), 
                    Vector3.zero,
                    rotation,
                    Vector3.one
                )
            );
        }
        
        

    }
    // changed to use the tilemap instead of the level map
    // because we need to handle rotations after completion
    // otherwise the quarters cannot interact with each other.
    int[,] CalculateAdjacentTiles(int tileX, int tileY)
    {
        var tile = tilemap.GetTile(new Vector3Int(tileX, tileY, 0));

        var bounds = tilemap.cellBounds;

        var yDim = bounds.size.x;
        var xDim = bounds.size.y;
        
        var adjacent = new int[3, 3];

        for (var x = 0; x <= 2; x++)
        for (var y = 0; y <= 2; y++)
        {
            var yPos = (tileY - 1) + y;
            var xPos = (tileX - 1) +x;
            // Debug.Log($"{x},{y} -> {xPos}, {yPos}");
            if (yPos < bounds.position.y || yPos >= yDim || xPos < bounds.position.x || xPos >= xDim)
            {
                
                adjacent[2-y,x] = -1;
                continue;
            }   

            if (!tilemap.HasTile(new Vector3Int(xPos, yPos, 0)))
            {
                adjacent[2-y,x] = -1;
                continue;
            }

            adjacent[2-y,x] = tileset.TileToMap(tilemap.GetTile(new Vector3Int(xPos, yPos, 0)));
        }
        
        return adjacent;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IsNotEmpty(int tile)
    {
        return tile != -1 && tile != TileType.Empty;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IsWall(int tile)
    {
        return tile is TileType.OutsideWall or TileType.OutsideCorner or TileType.InsideCorner or TileType.InsideWall;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    bool isExplicitWall(int tile)
    {
        return tile is TileType.OutsideWall or TileType.InsideWall;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IsCorner(int tile)
    {
        return tile is TileType.InsideCorner or TileType.OutsideCorner;
    }
    protected Quaternion CalculateRotationForTile(int x, int y)
    {
        var tile = tilemap.GetTile(new Vector3Int(x, y, 0));
        // rotations of walls 
        if (tile == tileset.outerWall)
        {
            var adj = CalculateAdjacentTiles(x, y);

            if (IsNotEmpty(adj[1,0])&& !IsWall(adj[0, 1]) && !IsWall(adj[2, 1]))
            {
                return Quaternion.Euler(0f, 0f, -90f);
            }

            if (adj[1, 0] == -1 && IsWall(adj[1, 2]))
            {
                return Quaternion.Euler(0f, 0f, -90f);
            }
        }
        if (tile == tileset.innerCorner)
        {
            var adj = CalculateAdjacentTiles(x, y);
            
            // ___
            // _wx
            // _x* 
            if (IsWall(adj[1, 2]) && IsWall(adj[2, 1]) && !IsWall(adj[2, 2]))
            {
                return Quaternion.Euler(0,0,0f);
            }
             
            // *x_
            // xw_
            // ___ 
            if (IsWall(adj[0, 1]) && IsWall(adj[1, 0]) && !IsWall(adj[0, 0]))
            {
                return Quaternion.Euler(0,0,180f);
            }
                

            // ___
            // xw_
            // *x_ 
            if (IsWall(adj[1, 0]) && IsWall(adj[2, 1]) && !IsWall(adj[2, 0]))
            {
                return Quaternion.Euler(0,0,-90);
            }
        }

        if (tile == tileset.outerCorner || tile == tileset.innerCorner)
        {
            var adj = CalculateAdjacentTiles(x, y);
            
            // Debug.Log(PrettyFormatMatrix(adj));
            // Debug.Log($"adj[0] = ${adj[0,0]} {adj[0, 1]} {adj[0,2]}");
            
            // _x_
            // _wx
            // ___
            if (IsWall(adj[0,1]) && IsWall(adj[1,2]))
                
            {
                return Quaternion.Euler(0f, 0f, 90f);
            }
            
            // ___
            // xw_
            // _x_
            
            if (IsWall(adj[1,0]) && IsWall(adj[2,1]))
            {
                return Quaternion.Euler(0f, 0f, -90f);
            }            
            //
            // // _x_
            // // _wx
            // // ___
            // if (IsWall(adj[0,1]) && IsWall(adj[1,2]))
            // {
            //     return Quaternion.Euler(0f, 0f, -90f);
            // }
            
            
            // ___
            // _wx
            // _x_
            if (IsWall(adj[1,2]) && IsWall(adj[2,1]))
            {
                return Quaternion.Euler(0f, 0f, 90f * 0);
            }
            
            // _x_
            // xw_
            // ___
            if (IsWall(adj[1,0]) && IsWall(adj[0,1]))
            {
                return Quaternion.Euler(0f, 0f, -180f);
            }            
        }

        if (tile == tileset.junction)
        {
            var adj = CalculateAdjacentTiles(x, y);
            if (!IsNotEmpty(adj[2, 1]))
            {
                return Quaternion.Euler(0f, 0f, 180f);
            }
            
        }

        if (tile == tileset.innerWall)
        {
            var adj = CalculateAdjacentTiles(x, y);
            
            // _x_
            // xwx
            // ___
            if (IsWall(adj[1, 0]) && IsWall(adj[1, 2]) && IsWall(adj[0,1]))
            {
                return Quaternion.Euler(0f, 0f, 90f);
            }
            // ___
            // xwx
            // _x_
            if (IsWall(adj[1, 0]) && IsWall(adj[1, 2]) && IsWall(adj[2,1]))
            {
                return Quaternion.Euler(0f, 0f, -90f);
            }
            
            if (IsWall(adj[0, 1]) || IsWall(adj[2,1])) return Quaternion.identity;
            if (IsWall(adj[1, 0]) || IsWall(adj[1,2]))
            {
                return Quaternion.Euler(0f, 0f, 90f);
            }
        }
        
        return Quaternion.identity;
    }

    private static string PrettyFormatMatrix(int[,] adj)
    {
        string dbg = "";
        for (var i = 0; i < adj.GetLength(0) * adj.GetLength(1); i += adj.GetLength(0))
        {
            var chunk = from int item in adj
                select item;
            var s = String.Join(", ", chunk.Skip(i).Take(adj.GetLength(0)).Select(r => r.ToString()));
            dbg += s + "\n";
        }

        return dbg;
    }

    private int[,] FlipMapX()
    {
        var xLen = levelMap.GetLength(1);
        var yLen = levelMap.GetLength(0);
        var target = new int[yLen, xLen];

        for (var y = 0; y < yLen; y++)
        {
            for (var x = 0; x < xLen; x++)
            {
                // Debug.Log($"Swapping {x},{y}={_levelMap[y,x]} with {xLen - x}, {y} = {_levelMap[y, xLen  - x - 1]}");
                var value = levelMap[y, xLen - x - 1];
                target[y, x] = value;
            }
        }

        return target;
    }
    private int[,] FlipMapY()
    {
        var xLen = levelMap.GetLength(1);
        var yLen = levelMap.GetLength(0);
        var target = new int[yLen, xLen];

        for (var y = 0; y < yLen; y++)
        {
            for (var x = 0; x < xLen; x++)
            {
                // Debug.Log($"Swapping {x},{y}={_levelMap[y,x]} with {xLen - x}, {y} = {_levelMap[y, xLen  - x - 1]}");
                target[y, x] = levelMap[yLen - y - 1, x];
            }
        }

        return target;
    }
    
    void Update()
    {
        if (generateMap)
        {
            tilemap.ClearAllTiles();

            GenerateLevelCorner(false, false);
            GenerateLevelCorner(true, false);
            GenerateLevelCorner(false,true);
            GenerateLevelCorner(true,true);
            FixAllTileRotations();


           generateMap = false;
        }

    }
    
    
}
