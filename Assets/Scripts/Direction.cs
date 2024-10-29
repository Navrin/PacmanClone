using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


public enum Direction : short
{
    North = 1 << 1,
    East = 1 << 2,
    South = 1 << 3,
    West = 1 << 4
}
[Flags]
public enum DirectionFlag : short
{
    None = 0,
    North = Direction.North,
    East = Direction.East,
    South = Direction.South,
    West = Direction.West,
}

public static class DirectionFlagMethods
{
    public static Direction[] GetDirections(this DirectionFlag flags)
    {
        var dirs = new List<Direction>();

        for (short i = 1, value = (short)DirectionFlag.North;
             value <= (short)DirectionFlag.West;
             i++, value = (short)(1 << i))
        {
            var masked = flags & (DirectionFlag)value;
            var dir = masked.ToDirection();
            if (dir.HasValue) dirs.Add(dir.Value);
        }

        return dirs.ToArray();
    }

    public static Direction? ToDirection(this DirectionFlag flag)
    {
        if (flag.HasFlag(DirectionFlag.North)) return Direction.North;
        if (flag.HasFlag(DirectionFlag.East)) return Direction.East;
        if (flag.HasFlag(DirectionFlag.South)) return Direction.South;
        if (flag.HasFlag(DirectionFlag.West)) return Direction.West;
        return null;
    }
}

public static class DirectionMethods
{
    public static readonly Array AllDirections =Direction.GetValues(typeof(Direction));

    public static Direction FromVec(Vector2Int vec)
    {
        return (vec.x, vec.y) switch
        {
            (0, 1) => Direction.North,
            (1, 0) => Direction.East,
            (0, -1) => Direction.South,
            (-1, 0) => Direction.West,
            _ => throw new ArgumentOutOfRangeException(nameof(vec), vec, null)
        };
    }
    public static Vector3 ToVec(this Direction direction)
    {
        return direction switch
        {
            Direction.North => new Vector3(0, 1, 0),
            Direction.East => new Vector3(1, 0, 0),
            Direction.South => new Vector3(0, -1, 0),
            Direction.West => new Vector3(-1, 0, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }
    public static Direction AntiDirection(this Direction direction)
    {
        return direction switch
        {
            Direction.North => Direction.South,
            Direction.East => Direction.West,
            Direction.South => Direction.North,
            Direction.West => Direction.East,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    public static Vector3Int ToIntVec(this Direction direction)
    {
        var dir = direction.ToVec();
        return new Vector3Int((int)dir.x, (int)dir.y, (int)dir.z);
    }

    public static float ToPSAngle(this Direction direction)
    {
        return direction switch
        {
            Direction.East => 0f,
            Direction.North => 90f,
            Direction.West => 180f,
            Direction.South => 270f,
        };
    }

    private static readonly int MoveNorth = Animator.StringToHash("MoveNorth");
    private static readonly int MoveEast = Animator.StringToHash("MoveEast");
    private static readonly int MoveSouth = Animator.StringToHash("MoveSouth");
    private static readonly int MoveWest = Animator.StringToHash("MoveWest");
    public static int AnimTrigger(this Direction direction)
    {
        return direction switch
        {
            Direction.North => MoveNorth,
            Direction.East => MoveEast,
            Direction.South => MoveSouth,
            Direction.West => MoveWest,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }
}
