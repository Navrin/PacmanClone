using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;

public class PacAudioController : MonoBehaviour
{
    public PacSFXCollection sfx;
    [FormerlySerializedAs("audioSource")] public AudioSource moveSource;
    public AudioSource audioOneShot;
    public PacStudentController controller;
    
    void Start()
    {
        moveSource ??= GetComponent<AudioSource>();
        // audioSource.loop = false;
        audioOneShot = gameObject.AddComponent<AudioSource>();
        audioOneShot.outputAudioMixerGroup = sfx.sfxSoundGroup;
        audioOneShot.playOnAwake = false;
        
        controller ??= GetComponent<PacStudentController>();
        controller.OnPacPickup += OnPickup;
        controller.OnPacCollision += OnCollide;
        // controller.OnPacMoveStart += OnMoveStart;
        controller.OnPacMoveEmpty += OnMoveEmpty;
        controller.OnPacDeath += OnDeath;
    }

    private void OnDeath()
    {
        moveSource.Stop();
        audioOneShot.PlayOneShot(sfx.death);
    }

    private void OnMoveEmpty(Vector3Int pos)
    {
        moveSource.mute = false;
        moveSource.Play();
    }

    private void OnMoveStart(Vector3Int pos)
    {
        moveSource.mute = false;
    }

    private void OnCollide(Vector3Int pos)
    {
        // Debug.Log($"Collision event invoked");
        moveSource.Stop();
        audioOneShot.PlayOneShot(sfx.collide);

    }
    
    

    private void OnPickup(Vector3Int pos, int kind)
    {
        moveSource.Stop();
        switch (kind)
        {
            case TileType.Pellet:
                audioOneShot.PlayOneShot(sfx.eat);
                break;
            case TileType.PowerUp:
                audioOneShot.PlayOneShot(sfx.powerup);
                break;
        }
    }
}
