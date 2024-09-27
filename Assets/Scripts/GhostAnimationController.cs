using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum GhostAnimationType
{
    None,
    CycleAll,
    Movement
}

public class GhostAnimationController : MonoBehaviour
{
    public Color baseColor;
    public float animationCycleTime;
    public Animator anim;
    public SpriteRenderer bodyRender;
    public SpriteRenderer eyeRender;
    
    public GhostAnimationType animationType;
    
    // Start is called before the first frame update
    void Start()
    {
        anim ??= GetComponent<Animator>();
        
        if (bodyRender is null) Debug.LogError($"{nameof(GhostAnimationController)} has no body render");
        if (eyeRender is null) Debug.LogError($"{nameof(GhostAnimationController)} has no eye render");

        if (bodyRender is not null)
        {
            bodyRender.color = baseColor;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
