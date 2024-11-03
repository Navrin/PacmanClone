using UnityEngine;
using Vector3 = UnityEngine.Vector3;


public class PacAnimController : MonoBehaviour
{
    private static readonly int Death = Animator.StringToHash("Death");
    private static readonly int MoveAbs = Animator.StringToHash("MoveAbs");
    private static readonly int Collision1 = Animator.StringToHash("Collision");
    private static readonly int Moving = Animator.StringToHash("Moving");
    private static readonly int ResetTrigger = Animator.StringToHash("Reset");


    public PacStudentController controller;
    
    public MoveTweener moveTweener;
    public Animator pacAnimator;

    public ParticleSystem moveParticles;
    
    // public AudioSource pacSound;
    // public bool shouldCycleAnimate = false;
    // public bool shouldDie = false;
    // internal Direction LastDirection;
    
    // Start is called before the first frame update
    void Start()
    {
        // moveTweener ??= GetComponent<MoveTweener>();
        controller ??= GetComponent<PacStudentController>();
        
        // moveTweener.OnTweenActive += OnTweenIsActive;
        // moveTweener.OnTweenComplete += OnTweenComplete;
        // moveTweener.OnTweenStart += OnTweenStart;
        
        // if (shouldCycleAnimate
        // {
        //     StartCoroutine(nameof(PacMoveCycle));
        // }
        // if (shouldDie)
        // {
        //     pacAnimator.SetTrigger(Death);
        // }
        // pacSound.mute = true;
        // pacSound.Play();

        controller.OnPacMoveStart += OnMoveStart;
        controller.OnPacCollision += OnMoveEnd;
        controller.OnPacAnyMove += OnAnyMovement;
        controller.OnPacDeath += OnPacDeath; 
        controller.OnPacReset += OnPacReset; 
    }

    private void OnDestroy()
    {
        
        controller.OnPacMoveStart -= OnMoveStart;
        controller.OnPacCollision -= OnMoveEnd;
        controller.OnPacAnyMove -= OnAnyMovement;
        controller.OnPacDeath -= OnPacDeath; 
        controller.OnPacReset -= OnPacReset; 

    }

    private void OnPacReset()
    {
        pacAnimator.SetTrigger(ResetTrigger);
    }

    private void OnPacDeath()
    {
        pacAnimator.SetTrigger(Death);
    }

    private void OnAnyMovement(Vector3Int pos)
    {
        pacAnimator.SetTrigger(controller.LastValidDirection.AnimTrigger());
        var sh = moveParticles.shape;
        
        sh.rotation = new Vector3(
            controller.LastValidDirection.ToPSAngle(),
            0,
            0
        );
    }

    void OnMoveStart(Vector3Int pos)
    {
        // pacSound.mute = false;
        // pacAnimator.SetFloat(MoveAbs, 1.0f);
        pacAnimator.SetTrigger(Moving);
        moveParticles.Play();
    }

    void OnMoveEnd(Vector3Int pos)
    {
        // pacAnimator.SetFloat(MoveAbs, 0.0f);
        // pacSound.mute = true;
        pacAnimator.SetTrigger(Collision1);
        var moveVec = controller.LastValidDirection.ToVec();
        
        var infront = transform.position + (moveVec * 0.5f) ;
        var emitParam = new ParticleSystem.EmitParams();
        emitParam.position = infront;
        emitParam.angularVelocity = 2.3f;
        
        moveParticles.Emit(emitParam, 50);
        moveParticles.Stop();
        

    }

    
    // IEnumerator PacMoveCycle()
    // {
    //     List<Direction> moveLoop = new List<Direction>
    //     {
    //         Direction.East,
    //         Direction.East,
    //         Direction.East,
    //         Direction.East,
    //         Direction.East,
    //         Direction.South,
    //         Direction.South,
    //         Direction.South,
    //         Direction.South,
    //         Direction.West,
    //         Direction.West,
    //         Direction.West,
    //         Direction.West,
    //         Direction.West,
    //         Direction.North,
    //         Direction.North,
    //         Direction.North,
    //         Direction.North
    //     };
    //
    //     while (true)
    //     {
    //         foreach (var move in moveLoop)
    //         {
    //             LastDirection = move;
    //             moveTweener.RequestMove(move);
    //             yield return new WaitUntil(moveTweener.TweenComplete);
    //         } 
    //     }
    // }
    
    
    
}
