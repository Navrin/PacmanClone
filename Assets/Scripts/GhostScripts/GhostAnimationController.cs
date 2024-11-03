using UnityEngine;

// OLD : From assessment 2
// [Serializable]
// public enum GhostAnimationType
// {
//     None,
//     CycleStates,
//     Movement,
//     CycleAll,
// }

public class GhostAnimationController : MonoBehaviour
{
    private static readonly int ScaredTrigger = Animator.StringToHash("Scared");
    private static readonly int DeadTrigger = Animator.StringToHash("Death");
    private static readonly int RecoveredTrigger = Animator.StringToHash("Recover");
    private static readonly int ReviveTrigger = Animator.StringToHash("Revive");
    
    public Animator anim;

    public GhostController controller;
    // public SpriteRenderer bodyRender;
    // public SpriteRenderer eyeRender;
    public MoveTweener moveTweener;
    // public float ghostScaredTime = 3f;
    
    // Direction _lastDirection;

    void Start()
    {
        anim ??= GetComponent<Animator>();
        moveTweener ??= GetComponent<MoveTweener>();
        controller ??= GetComponent<GhostController>();
        // moveTweener.OnTweenStart += OnTweenStart;
  
        controller.OnGhostDirectionChange += DirectionChange;
        controller.OnGhostDead += OnDeathEvent;
        controller.OnGhostScared += OnGhostScared;
        controller.OnGhostRecovered += OnGhostRecovered;
        controller.OnGhostRevive += OnGhostRevive;
    }

    public void OnDestroy()
    {
        
        controller.OnGhostDirectionChange -= DirectionChange;
        controller.OnGhostDead -= OnDeathEvent;
        controller.OnGhostScared -= OnGhostScared;
        controller.OnGhostRecovered -= OnGhostRecovered;
        controller.OnGhostRevive -= OnGhostRevive;
    }

    private void OnGhostRevive()
    {
        // anim.SetTrigger(ReviveTrigger);
        anim.SetLayerWeight(2, 0.0f);
        anim.SetLayerWeight(1, 1.0f);
    }

    private void OnGhostRecovered()
    {
        anim.SetTrigger(RecoveredTrigger);
    }

    private void OnGhostScared()
    {
        anim.SetTrigger(ScaredTrigger);
    }

    private void OnDeathEvent()
    {
        // anim.SetTrigger(DeadTrigger);
        anim.SetLayerWeight(2, 1.0f);
        anim.SetLayerWeight(1, 0.0f);
    }
    
    

    private void DirectionChange(Direction dir)
    {
        anim.SetTrigger(dir.AnimTrigger());
    }
}
