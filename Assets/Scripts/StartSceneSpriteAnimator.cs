using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class StartSceneSpriteAnimator : MonoBehaviour
{
    public GameObject pacStudent;
    public GameObject[] ghosts;
    private List<GhostAnimationController> _ghostControllers = new List<GhostAnimationController>();
    private GameObject _pacSprite;
    private MoveTweener _pacTweener;
    private Animator _pacAnimator;
    private Quaternion _rotate;
    
    private Camera _cam;
    private Bounds _bounds;
    // Start is called before the first frame update
    void Start()
    {
        _pacTweener = pacStudent.GetComponent<MoveTweener>();
        _pacAnimator = pacStudent.GetComponent<Animator>();
        _pacSprite = pacStudent.transform.GetChild(0).gameObject;
        _rotate = new Quaternion();
        _pacAnimator.SetFloat("MoveAbs", 1f);
        _pacAnimator.SetBool("DirectionManaged", true);
        
        _cam = Camera.main!;
        _bounds = new Bounds();
        foreach (var ghost in ghosts)
        {
            var controller = ghost.GetComponent<GhostAnimationController>();
            controller.moveTweener.OnTweenStart = null;
            controller.anim.SetTrigger("MoveEast");
            _ghostControllers.Add(controller);
        }
        
        _bounds.SetMinMax(
            _cam.ScreenToWorldPoint(Vector2.zero),
            _cam.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height))
        );
        Debug.Log(_bounds);
        _bounds.Expand(20);
        StartCoroutine(PacAnimationCycle());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator PacAnimationCycle()
    {
        while (gameObject.activeSelf)
        {
            var startPos = RandomPositionOutsideScreen();
            pacStudent.transform.position = startPos;

            var time = Random.Range(3f, 6f);

            var endPos = new Vector3(-startPos.x, -startPos.y, 0);
            var subVec  = endPos - startPos;
            var rot = Mathf.Atan2(subVec.y, subVec.x) * Mathf.Rad2Deg;

            var ghostRandom = Random.Range(0f, 1.0f);
            if (ghostRandom > 0.5f)
            {
                var mult = 1.1f;
                
                foreach (var ghost in _ghostControllers)
                {
                    var behind = startPos * mult;
                    
                    ghost.transform.position = behind;
                    ghost.transform.rotation = Quaternion.Euler(0,0,rot);
                    ghost.moveTweener.RequestMove(
                        endPos, time * mult
                    );
                    mult *= 1.05f;
                }

            }
            
            pacStudent.transform.rotation = Quaternion.Euler(0, 0, rot);
            
            _pacTweener.RequestMove(endPos, time);
            // _rotate.SetFromToRotation(pacStudent.transform.position, endPos);
            Debug.Log(                Vector3.Angle(startPos, endPos));
            Debug.Log($"{pacStudent.transform.position} -> {endPos} = {pacStudent.transform.rotation.eulerAngles}");
            
            yield return new WaitUntil(_pacTweener.TweenComplete);
            if (ghostRandom > 0.5f)
            {
                yield return new WaitUntil(_ghostControllers.Last().moveTweener.TweenComplete);
            }
            yield return new WaitForSeconds(Random.Range(1f, 3f));
        }
    }

    private Vector3 RandomPositionOutsideScreen()
    {
        var randomAngle = Random.Range(0f, 2 * Mathf.PI);
        var point = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
        Debug.Log($"{point} {_bounds.extents.ToString()}");
        point *= _bounds.extents;
        var bounded = _bounds.ClosestPoint(point);

        return bounded;
    }

}
