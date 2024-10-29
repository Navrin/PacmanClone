using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public abstract class GhostBehaviour
{
    public LevelMapUtil LevelUtil = LevelMapUtil.GetInstance();
    
    public virtual Direction NextMovement(Vector2Int currentPos, Vector2Int playerPos, Direction currentDirection) 
    {
        var inSpawnMove = NavigateOutOfSpawn(currentPos, currentDirection);
        if (inSpawnMove.HasValue) return inSpawnMove.Value;
        
        var movements = LevelUtil.GetValidMovements(currentPos.x, currentPos.y);
        if (currentDirection != 0)
        {
            if (!RemoveBackstep(currentDirection, ref movements)) Debug.Log($"Backstep returned false for {currentDirection}"); 
        }

        var move = (short)movements;

        (Direction, float)? bestDirection = null;
        
        foreach (Direction direction in DirectionMethods.AllDirections)
        {
            
            var dir = (Direction)(move & (short)direction);

            if (dir == 0) continue;

            var dirVec = dir.ToIntVec();
            
            var nextPos = currentPos + (Vector2Int)dirVec;
            if (!LevelUtil.GhostSpawnArea.Contains((Vector3Int)currentPos))
            {
                if (LevelUtil.GhostSpawnArea.Contains((Vector3Int)nextPos)) continue;
            }
            if (CheckMoveOutBounds(nextPos)) continue; // todo: check this, might cause bugs later
            CalculateMovementHeuristic(
                nextPos, playerPos, direction, ref bestDirection
            );
        }
        
        if (!bestDirection.HasValue)
        {
            throw new Exception($"error no valid directions found for {currentPos}");
        }
        return bestDirection.Value.Item1;
    }

    public bool CheckMoveOutBounds(Vector2Int nextPos)
    {
        var outsideLeftBounds = nextPos.x < LevelUtil.cellBounds.xMin;
        var outsideRightBounds = nextPos.x >= LevelUtil.cellBounds.xMax;
        return outsideLeftBounds || outsideRightBounds;
    }

    /**
     * @returns false if removing backstep would remove all possible movements
     */
    public bool RemoveBackstep(Direction currentDirection, ref DirectionFlag flag)
    {
        if (currentDirection == 0) return false;
        var anti = (DirectionFlag) currentDirection.AntiDirection();
        var masked = flag ^ anti;
        if (masked == DirectionFlag.None) return false;

        flag ^= anti;
        return true;
    }

    public Vector2Int GetPositionInTilemap(Transform transform)
    {
        return (Vector2Int)LevelUtil.GetPosFromTransform(transform);
    }

    public virtual void CalculateMovementHeuristic(
        Vector2Int nextPos, 
        Vector2Int playerPos,
        Direction nextDir, 
        ref (Direction, float)? currentBest)
    {
        var dist = Vector2Int.Distance(nextPos, playerPos);
        if (currentBest == null) currentBest = (nextDir, dist);
        else if (dist > currentBest.Value.Item2) currentBest = (nextDir, dist);
        
    }

    public Direction? NavigateOutOfSpawn(Vector2Int currentPos, Direction currentDirection)
    {
        if (LevelUtil.GhostSpawnArea.Contains((Vector3Int)currentPos))
        {
            Stack<Vector2Int> points = new Stack<Vector2Int>();
            var path = LevelUtil.NavigateTowardsGoal(
                currentPos, 
                new Vector2Int(0, currentPos.y > 0 ? 3 : -3),
                ref points, 
                currentDirection != 0 ? currentDirection.AntiDirection() : null,
                blockSpawnArea: false    
            );
            
            if (points.Count > 0) points.Pop();
            if (points.Count > 0)
            {
                return DirectionMethods.FromVec(points.Peek() - currentPos);
            }
        }

        return null;
    }
}

class Ghost1Behaviour : GhostBehaviour
{
}

class Ghost2Behaviour : GhostBehaviour
{
    public override void CalculateMovementHeuristic(
        Vector2Int nextPos, 
        Vector2Int playerPos,
        Direction nextDir, 
        ref (Direction, float)? currentBest)
    {
        var dist = Vector2Int.Distance(nextPos, playerPos);
        if (currentBest == null) currentBest = (nextDir, dist);
        else if (dist < currentBest.Value.Item2) currentBest = (nextDir, dist);
        
    }
}
class Ghost4Behaviour : GhostBehaviour
{
    private Stack<Vector2Int> _points = new Stack<Vector2Int>();

    private readonly Vector2Int[] _borderPoints = new Vector2Int[]
    {
        new(7, 0),
        new(7, -6),
        new(12, -6),
        new(12, -13),
        new(1, -13),
        new(-2, -13),
        new(-13, -13),
        new(-13, -6),
        new(-8, -6),
        new(-8, 6),
        new(-13, 6),
        new(-13, 13),
        new(-2, 13),
        new(1, 13),
        new(12, 13),
        new(12, 6),
        new(7, 6),
    };
    
    
    LinkedList<Vector2Int> targets = new LinkedList<Vector2Int>();

    public Ghost4Behaviour()
    {
        foreach (var p in _borderPoints)
        {
            targets.AddLast(p);
        }
    }


    private Vector2Int GetNextTarget()
    {
        var prior = targets.First.Value;
        targets.RemoveFirst();
        targets.AddLast(prior);
        return prior;
    }

    private void FindClosestTarget(Vector2Int pos)
    {
        targets.Clear();
        var closest = _borderPoints.Aggregate(
            (a, b) =>  
                Vector2Int.Distance(pos, a) < Vector2Int.Distance(pos, b) ? a : b
        );
        
        var index = Array.IndexOf(_borderPoints, closest);
        // Debug.Log($"{index}");
        var post = _borderPoints[index..];
        
        foreach (var el in post) targets.AddLast(el);
        if (index > 0)
        {
            var pre = _borderPoints[..(index-1)];
            foreach (var el in pre) targets.AddLast(el);
        }
        
    }
    public override Direction NextMovement(
        Vector2Int currentPos, Vector2Int playerPos, Direction currentDirection)    
    {
        
        // if (LevelUtil.GhostSpawnArea.Contains((Vector3Int)currentPos))
        // {
        
        // Debug.Log($"currentpos {currentPos} in spawn {LevelUtil.GhostSpawnArea.Contains((Vector3Int)currentPos)}");
        var inSpawn = NavigateOutOfSpawn(currentPos, currentDirection);
        if (inSpawn.HasValue) return inSpawn.Value;
        
        if (_points.Count > 0 && _points.Peek() != currentPos)
        {
            // Debug.Log($"Ghost was interrupted, recalculating path");
            _points.Clear();
            FindClosestTarget(currentPos);
        }

        if (_points.Count <= 0)
        {
            var foundPath = LevelUtil.NavigateTowardsGoal(
                currentPos, GetNextTarget(), ref _points, currentDirection != 0 ? currentDirection.AntiDirection() : null
                
                // LevelUtil.GhostSpawnArea.Contains((Vector3Int)playerPos) ? null : ((a, b) =>
                // {
                //     if (LevelUtil.GhostSpawnArea.Contains((Vector3Int)b))
                //     {
                //         return float.PositiveInfinity;
                //     }
                //
                //     return Vector2Int.Distance(a, b);
                // })
            );
            // Debug.Log($"{String.Join(", ", points.Select(x => x.ToString()))}, path: {foundPath}");
        }

        if (_points.Peek() == currentPos)
            _points.Pop();
        // we have hit the end of the path
        if (_points.Count <= 0) return NextMovement(currentPos, playerPos, currentDirection);
        
        var dir = _points.Peek() - currentPos;
        if (dir.magnitude > 1) return NextMovement(currentPos, playerPos, currentDirection);
        // Debug.Log($"next dir {dir}");
        if (currentDirection == 0) return DirectionMethods.FromVec(dir);
        
        var anti = currentDirection.AntiDirection().ToIntVec();
        if (anti.x != dir.x || anti.y != dir.y) return DirectionMethods.FromVec(dir);

        _points.Clear();
        return NextMovement(currentPos, playerPos, currentDirection);
    }
}

class Ghost3Behaviour : GhostBehaviour
{
    public override Direction NextMovement(Vector2Int currentPos, Vector2Int playerPos, Direction currentDirection)
    {
        var inSpawn = NavigateOutOfSpawn(currentPos, currentDirection);
        if (inSpawn.HasValue) return inSpawn.Value;
        
        var valid = LevelUtil.GetValidMovements(currentPos.x, currentPos.y);
        RemoveBackstep(currentDirection, ref valid);

        var dirs = valid.GetDirections().Where(r => !CheckMoveOutBounds(currentPos + (Vector2Int)r.ToIntVec())).ToList();

        if (!LevelUtil.GhostSpawnArea.Contains((Vector3Int)currentPos))
        {
            foreach (var d in dirs.Where(d => LevelUtil.GhostSpawnArea.Contains((Vector3Int)currentPos + d.ToIntVec())).ToList())
            {
                dirs.Remove(d);
            }
        }

        return dirs.Count == 0 ? Direction.East : dirs[Random.Range(0, dirs.Count - 1)];
    }
}
public class GhostController : MonoBehaviour
{
    public enum GhostState
    {
        Normal,
        Scared,
        Recovering,
        Inactive,
    }

    private bool Dead { get; set; }

    public GhostAnimationController animController;
    public MoveTweener moveTweener;
    public TMP_Text identifierText;
    public SpriteRenderer sprite;
    public GameObject managers;

    public int ghostIdentifier;

    [FormerlySerializedAs("ghostProps")] public GhostProperties props;

    internal LevelStateManager _levelState;

    public GhostState State { get; private set; }

    private Direction _lastDirection;
    


    public delegate void OnGhostDeadEvent();

    public event OnGhostDeadEvent OnGhostDead;

    public delegate void OnGhostScaredEvent();

    public event OnGhostScaredEvent OnGhostScared;

    public delegate void OnGhostRecoveredEvent();

    public event OnGhostRecoveredEvent OnGhostRecovered;

    public delegate void OnGhostReviveEvent();

    public event OnGhostReviveEvent OnGhostRevive;

    public delegate void OnGhostDirectionChangeEvent(Direction newDirection);

    public event OnGhostDirectionChangeEvent OnGhostDirectionChange;

    public GhostBehaviour GhostBehaviour;
    
    public GhostBehaviour[] GhostBehaviours = new GhostBehaviour[]
    {
        new Ghost1Behaviour(),
        new Ghost2Behaviour(),
        new Ghost3Behaviour(),
        new Ghost4Behaviour(),
    };
    

    public void Spawn()
    {
        animController ??= GetComponent<GhostAnimationController>();
        moveTweener ??= GetComponent<MoveTweener>();
        SyncProps();
        AddBehaviour();

        managers ??= StartManager.instance;
        _levelState ??= managers.GetComponent<LevelStateManager>();

        _levelState.OnGhostScared += OnPowerUpCollected;
        _levelState.OnGameActive += StartGhostMovement;
        _levelState.OnLifeChange += HaltGhostMovement;
        _levelState.OnGameRestart += HaltGhostMovement;
        _levelState.OnGameOver += HaltGhostMovement;
        moveTweener.OnTweenComplete += CalculateNext;
        State = GhostState.Inactive;
    }

    private void HaltGhostMovement(int lives)
    {
        HaltGhostMovement();
    }


    public void SyncProps()
    {
        sprite.color = props.ghostColors[ghostIdentifier];
        identifierText.text = (1 + ghostIdentifier).ToString();
    }

    void AddBehaviour()
    {
        if (ghostIdentifier < GhostBehaviours.Length)
        {
            GhostBehaviour = GhostBehaviours[ghostIdentifier];
        }
    }

    public void OnDestroy()
    {
        State = GhostState.Inactive;
        if (moveTweener)
            moveTweener.OnTweenComplete -= CalculateNext;
        StopAllCoroutines();
        if (_levelState)
        {
            _levelState.OnGhostScared -= OnPowerUpCollected;
            _levelState.OnGameActive -= StartGhostMovement;
            _levelState.OnGameRestart -= HaltGhostMovement;
            _levelState.OnGameOver -= HaltGhostMovement;
            _levelState.OnLifeChange -= HaltGhostMovement;
        }



    }


    private void OnPowerUpCollected()
    {
        // What should happen if a powerup is collected while already in a powerup state?
        StopCoroutine(nameof(GhostScaredBehaviour));
        StartCoroutine(nameof(GhostScaredBehaviour));
    }

    IEnumerator GhostScaredBehaviour()
    {
        
        State = GhostState.Scared;
        OnGhostScared?.Invoke();
        GhostBehaviour = GhostBehaviours[0];
        
        yield return new WaitForSeconds(7);
        OnGhostRecovered?.Invoke();
        State = GhostState.Recovering;
        yield return new WaitForSeconds(3);
        // todo: something here?
        State = GhostState.Normal;
        AddBehaviour();
    }

    public void GhostDead()
    {
        GhostBehaviour = GhostBehaviours[0];
        Dead = true;
        OnGhostDead?.Invoke();
        StopCoroutine(nameof(GhostDeadBehaviour));
        StartCoroutine(nameof(GhostDeadBehaviour));
    }

    IEnumerator GhostDeadBehaviour()
    {
        moveTweener.RequestMoveForce(
            _levelState.tilemap.GetCellCenterWorld(new Vector3Int(0, 0, 0)),
            moveTweener.moveTime * Vector2.Distance(transform.position, new Vector2(0,0))
        );

        yield return new WaitUntil(moveTweener.TweenComplete);
        GhostRevive();
    }

    public void GhostRevive()
    {
        Dead = false;
        OnGhostRevive?.Invoke();
        CalculateNextMove();
    }

    void StartGhostMovement()
    {
        State = GhostState.Normal;

        CalculateNextMove();
    }

    private void CalculateNextMove()
    {
        if (GhostBehaviour is null) return;
        if (State == GhostState.Inactive || Dead) return;

        var next = GhostBehaviour.NextMovement(
            GhostBehaviour.GetPositionInTilemap(transform),
            (Vector2Int)_levelState.CurrentPacPosition,
            _lastDirection
        );
        
        moveTweener.RequestMove(next);
        _lastDirection = next;
        OnGhostDirectionChange?.Invoke(next);
    }

    private void CalculateNext()
    {
        if (State == GhostState.Inactive) return;
        
        CalculateNextMove();
    }
    
    void HaltGhostMovement()
    {
        State = GhostState.Inactive;
    }

}
