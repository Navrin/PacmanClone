using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum GhostAnimationType
{
    None,
    CycleStates,
    Movement,
    CycleAll,
}

public class GhostAnimationController : MonoBehaviour
{
    private static readonly int Scared = Animator.StringToHash("Scared");
    private static readonly int IsDead = Animator.StringToHash("IsDead");
    public Color baseColor;
    public float animationCycleTime;
    public Animator anim;
    public SpriteRenderer bodyRender;
    public SpriteRenderer eyeRender;
    public MoveTweener moveTweener;
    public GhostAnimationType animationType;
    public float ghostScaredTime = 3f;
    
    Direction _lastDirection;

    // private bool _ghostScared = false;
    // public bool GhostScared
    // {
        // get => _ghostScared;
        // set
        // {
           // _ghostScared = value;
           // anim.SetBool(Scared, _ghostScared);
        // }
    // }

    private bool _ghostDead = false;

    public bool GhostDead
    {
        get => _ghostDead;
        set
        {
            _ghostDead = value;
            anim.SetBool(IsDead, _ghostDead);
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        anim ??= GetComponent<Animator>();
        moveTweener ??= GetComponent<MoveTweener>();

        moveTweener.OnTweenStart += OnTweenStart;
        
        if (bodyRender is null) Debug.LogError($"{nameof(GhostAnimationController)} has no body render");
        if (eyeRender is null) Debug.LogError($"{nameof(GhostAnimationController)} has no eye render");

        if (bodyRender is not null)
        {
            bodyRender.color = baseColor;
        }

        if (animationType is GhostAnimationType.Movement or GhostAnimationType.CycleAll)
        {
            StartCoroutine(nameof(GhostMoveLoop));
        }

        if (animationType is GhostAnimationType.CycleStates or GhostAnimationType.CycleAll)
        {
            StartCoroutine(nameof(GhostStateLoop));
        }
        
    }

    IEnumerator GhostStateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(animationCycleTime);
            yield return TriggerGhostScared();
            TriggerGhostDead();
            yield return new WaitForSeconds(animationCycleTime);
            TriggerGhostRespawn();
        }
    }

    /**
     * !NOTE: This might have some adverse effects when transitioning
     * from dead state. Is the ghost meant to go back to scaredy state
     * if it respawns while the other ghosts are scared?
     */
    public IEnumerator TriggerGhostScared()
    {
        anim.SetTrigger(Scared);

        yield return new WaitForSeconds(ghostScaredTime);
        // GhostScared = false;
        
    }

    /**
     * Ghost respawn should be triggered by an external event,
     * i.e. ghost is in the respawn area.
     */
    public void TriggerGhostDead()
    {
        GhostDead = true;
    }

    public void TriggerGhostRespawn()
    {
        GhostDead = false;
    }
    
    void OnTweenStart()
    {
        anim.SetTrigger(_lastDirection.AnimTrigger());
    }
    
    
    IEnumerator GhostMoveLoop()
    {
        List<Direction> moveLoop = new List<Direction>
        {
            Direction.East,
            Direction.East,
            Direction.South,
            Direction.South,
            Direction.West,
            Direction.West,
            Direction.North,
            Direction.North
        };

        while (true)
        {
            foreach (var move in moveLoop)
            {
                _lastDirection = move;
                moveTweener.RequestMove(move);
                yield return new WaitUntil(moveTweener.TweenComplete);
            } 
        }
    }
}
