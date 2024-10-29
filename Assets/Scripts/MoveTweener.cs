using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class TweenRequest
{
    public Vector3 StartPos { get; private set; }
    public Vector3 EndPos {get; private set;}
    public float TimeStart { get; private set; }
    public float MoveTime { get; private set; }

    internal bool halfCompleteInvoked = false;

    public TweenRequest(Vector2 startPos, Vector2 endPos, float timeStart, float moveTime)
    {
        this.StartPos = startPos;
        this.EndPos = endPos;
        this.TimeStart = timeStart;
        this.MoveTime = moveTime;
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
    public delegate void OnTweenHalfCompleteDelegate();
    public event OnTweenHalfCompleteDelegate OnTweenHalfComplete;
 
    
    public void RequestMove(Direction direction)
    {
        if (_activeTween != null) return;

        RequestMove(moveTarget.position + direction.ToVec(), moveTime);
    }
    
    public void RequestMove(Vector2 position)
    {
        if (_activeTween != null) return;

        RequestMove(position, moveTime);
    }

    public void RequestMove(Vector2 position, float time)
    {
        if (_activeTween != null) return;

        // change to enqueue 
        RequestMoveForce(position, time);
        
    }

    public void RequestMoveForce(Vector2 position, float time)
    {
        _activeTween = new TweenRequest(
            moveTarget.position,
            position,
            Time.time,
            time
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

    public Vector3 GetTweenTarget()
    {
        return _activeTween.EndPos;
    }
    

    // Update is called once per frame
    void FixedUpdate()
    {
        
        if (_activeTween != null)
        {
            var timeNorm = (Time.time - _activeTween.TimeStart) / _activeTween.MoveTime;
            var nextPos = Vector3.Lerp(
                _activeTween.StartPos, 
                _activeTween.EndPos, 
                timeNorm
            );
            
            moveTarget.position = nextPos;
            OnTweenActive?.Invoke();

            if (!_activeTween.halfCompleteInvoked && timeNorm > 0.5f)
            {
                OnTweenHalfComplete?.Invoke();
                _activeTween.halfCompleteInvoked = true;
            }
            
            
            if (Time.time - _activeTween.TimeStart > _activeTween.MoveTime)
            {
                _activeTween = null;
                OnTweenComplete?.Invoke();
                return;
            }
        }
    }

    public void ForceStop()
    {
        _activeTween = null;
        
    }
}
