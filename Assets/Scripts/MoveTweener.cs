using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class TweenRequest
{
    public Vector3 StartPos { get; private set; }
    public Vector3 EndPos {get; private set;}
    public float TimeStart { get; private set; }

    public TweenRequest(Vector2 startPos, Vector2 endPos, float timeStart)
    {
        this.StartPos = startPos;
        this.EndPos = endPos;
        this.TimeStart = timeStart;
    }
}

public enum Direction
{
    North,
    East,
    South,
    West
}

public static class DirectionMethods
{
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
public class MoveTweener : MonoBehaviour
{
    public Transform moveTarget;
    public float moveTime = 0.3f;
    private TweenRequest _activeTween;

    public delegate void OnTweenStartDelegate();
    /**
     * Called on tween startup
     */
    public OnTweenStartDelegate OnTweenStart;
    
    public delegate void OnTweenActiveDelegate();
    /**
     * Called every fixed update while tween is active
     */
    public OnTweenActiveDelegate OnTweenActive;
    public delegate void OnTweenCompleteDelegate();
    /**
     * Called on tween complete. 
     */
    public OnTweenCompleteDelegate OnTweenComplete;
    
    // Start is called before the first frame update
 
    
    public void RequestMove(Direction direction)
    {
        // change to enqueue 
        if (_activeTween != null) return;
        
        _activeTween = new TweenRequest(
            moveTarget.position,
            moveTarget.position + direction.ToVec(),
            Time.time
        );
        OnTweenStart?.Invoke();
    }

    
    void Start()
    {
        moveTarget ??= transform;
    }
    
    public bool TweenActive()
    {
        return _activeTween is not null;
    }

    public bool TweenComplete()
    {
        return _activeTween is null;
    }
    
    

    // Update is called once per frame
    void FixedUpdate()
    {
        
        if (_activeTween != null)
        {
            
            var nextPos = Vector3.Lerp(
                _activeTween.StartPos, 
                _activeTween.EndPos, 
                (Time.time - _activeTween.TimeStart) / moveTime
            );
            
            moveTarget.position = nextPos;
            OnTweenActive?.Invoke();
            
            if (Time.time - _activeTween.TimeStart > moveTime)
            {
                _activeTween = null;
                OnTweenComplete?.Invoke();
            }
        }
    }
}