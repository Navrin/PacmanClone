using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

class TweenRequest
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

enum Direction
{
    North,
    East,
    South,
    West
}

static class DirectionMethods
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

    
    public static String AnimTrigger(this Direction direction)
    {
        return direction switch
        {
            Direction.North => "MoveNorth",
            Direction.East => "MoveEast",
            Direction.South => "MoveSouth",
            Direction.West => "MoveWest",
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }
}

public class PacMovementHandler : MonoBehaviour
{
 

    private TweenRequest _activeTween;
    public Transform moveTarget;
    public Animator pacAnimator;
    public AudioSource pacSound;
    public float moveTime = 0.3f;
    
    // Start is called before the first frame update
    void Start()
    {

        StartCoroutine(nameof(PacMoveCycle));
    }

    // Update is called once per frame
    void Update()
    {
        if (_activeTween != null)
        {
            if (!pacSound.isPlaying) pacSound.Play();
            pacAnimator.SetFloat("MoveAbs", 1.0f);
            
            var nextPos = Vector3.Lerp(
                _activeTween.StartPos, 
                _activeTween.EndPos, 
                (Time.time - _activeTween.TimeStart) / moveTime
            );
            
            moveTarget.position = nextPos;
            if (Time.time - _activeTween.TimeStart > moveTime)
            {
                _activeTween = null;
                pacAnimator.SetFloat("MoveAbs", 0.0f);
                pacSound.Stop();
            }
        }
    }

    IEnumerator PacMoveCycle()
    {
        List<Direction> moveLoop = new List<Direction>();
        moveLoop.Add(Direction.East);
        moveLoop.Add(Direction.East);
        moveLoop.Add(Direction.East);
        moveLoop.Add(Direction.East);
        moveLoop.Add(Direction.East);
        moveLoop.Add(Direction.South);
        moveLoop.Add(Direction.South);
        moveLoop.Add(Direction.South);
        moveLoop.Add(Direction.South);
        moveLoop.Add(Direction.West);
        moveLoop.Add(Direction.West);
        moveLoop.Add(Direction.West);
        moveLoop.Add(Direction.West);
        moveLoop.Add(Direction.West);
        moveLoop.Add(Direction.North);
        moveLoop.Add(Direction.North);
        moveLoop.Add(Direction.North);
        moveLoop.Add(Direction.North);

        while (true)
        {
            foreach (var move in moveLoop)
            {
                RequestMove(move);
                yield return new WaitUntil(() => _activeTween == null);
            } 
        }
    }
    
    
    void RequestMove(Direction direction)
    {
        // change to enqueue 
        // nvm, animation should be cancellable
        // if (_activeTween != null) return;
        
        _activeTween = new TweenRequest(
            moveTarget.position,
            moveTarget.position + direction.ToVec(),
            Time.time
        );

        pacAnimator.SetTrigger(direction.AnimTrigger());
    }
}
