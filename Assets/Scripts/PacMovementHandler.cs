using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


public class PacMovementHandler : MonoBehaviour
{
    private static readonly int Death = Animator.StringToHash("Death");
    private static readonly int MoveAbs = Animator.StringToHash("MoveAbs");

    public MoveTweener moveTweener;
    public Animator pacAnimator;
    public AudioSource pacSound;
    public bool shouldCycleAnimate = false;
    public bool shouldDie = false;

    private Direction _lastDirection;
    
    // Start is called before the first frame update
    void Start()
    {
        moveTweener ??= GetComponent<MoveTweener>();
        moveTweener.OnTweenActive += OnTweenIsActive;
        moveTweener.OnTweenComplete += OnTweenComplete;
        moveTweener.OnTweenStart += OnTweenStart;
        
        if (shouldCycleAnimate)
        {
            StartCoroutine(nameof(PacMoveCycle));
        }
        else if (shouldDie)
        {
            pacAnimator.SetTrigger(Death);
        }
    }

    void OnTweenIsActive()
    {
        if (!pacSound.isPlaying) pacSound.Play();
        pacAnimator.SetFloat(MoveAbs, 1.0f);
    }

    void OnTweenComplete()
    {
        pacAnimator.SetFloat(MoveAbs, 0.0f);
        pacSound.Stop();

    }

    void OnTweenStart()
    {
        pacAnimator.SetTrigger(_lastDirection.AnimTrigger());
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
                _lastDirection = move;
                moveTweener.RequestMove(move);
                yield return new WaitUntil(moveTweener.TweenActive);
            } 
        }
    }
    
    
    
}
