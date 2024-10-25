using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;


public class GhostController : MonoBehaviour
{
    public enum GhostState
    {
        Normal,
        Scared,
        Dead,
        Recovering,
    }
    
    public GhostAnimationController animController;
    public TMP_Text identifierText;
    public SpriteRenderer sprite;
    public GameObject managers;
    
    public int ghostIdentifier;

    [FormerlySerializedAs("ghostProps")] public GhostProperties props;
    
    internal LevelStateManager _levelState;
    
    public GhostState State { get; private set; }
    

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
    
    
    
    public void Spawn()
    {
       animController ??= GetComponent<GhostAnimationController>(); 
       SyncProps();

       managers ??= StartManager.instance;
       _levelState ??= managers.GetComponent<LevelStateManager>();
       
       _levelState.OnGhostScared += OnPowerUpCollected;
    }

    public void SyncProps()
    {
        sprite.color = props.ghostColors[ghostIdentifier];
        identifierText.text = (1+ghostIdentifier).ToString();
    }

    public void OnDestroy()
    {
        StopAllCoroutines();
        if (_levelState)
            _levelState.OnGhostScared -= OnPowerUpCollected;
    }


    private void OnPowerUpCollected()
    {
        // What should happen if a powerup is collected while already in a powerup state?
        StopCoroutine(nameof(GhostScaredBehaviour));
        StartCoroutine(nameof(GhostScaredBehaviour));
    }

    IEnumerator GhostScaredBehaviour()
    {
        OnGhostScared?.Invoke();
        yield return new WaitForSeconds(7);
        OnGhostRecovered?.Invoke();
        yield return new WaitForSeconds(3);
        // todo: something here?
        StopCoroutine(nameof(GhostScaredBehaviour));
    }

    public void GhostDead()
    {
        OnGhostDead?.Invoke();
        // todo: change AI behaviour
    }

    public void GhostRevive()
    {
        OnGhostRevive?.Invoke();
    }
}
