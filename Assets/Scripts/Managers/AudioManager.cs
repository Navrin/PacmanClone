using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource normalTrack;
    public AudioSource scaredTrack;
    public AudioSource ghostDownTrack;
    public AudioSource ghostDownScaredTrack;
    public AudioSource introTrack;
    List<AudioSource> _tracks = new List<AudioSource>();

    public GameObject managers;

    public LevelStateManager levelState;
    
    
    // Start is called before the first frame update
    void Awake()
    {
        managers = GameObject.FindWithTag("Managers");
        levelState ??= managers.GetComponent<LevelStateManager>(); 
        _tracks.Add(normalTrack);
        _tracks.Add(scaredTrack);
        _tracks.Add(ghostDownTrack);
        _tracks.Add(ghostDownScaredTrack);

        
        StartCoroutine(WaitForIntro());
        
        levelState.OnGhostScared += GhostScared;
        levelState.OnGhostEliminated += GhostElim;
        levelState.OnGhostRecovered += GhostRecovered;
        levelState.OnGhostRevived += GhostRevived;
        levelState.OnLifeChange += PacDeath;
        levelState.OnGameRestart += OnRound;
    }

    private void OnRound()
    {
        StartCoroutine(WaitForIntro());
    }

    private void PacDeath(int lives)
    {
        MuteOthers(normalTrack);
        foreach (var track in _tracks)
            track.Stop();
    }

    private void GhostRevived(GameObject ghost)
    {
        if (levelState.AnyGhostsDead && levelState.GhostScaredRemainingTime > 0) MuteOthers(ghostDownScaredTrack);
        else if (!levelState.AnyGhostsDead && levelState.GhostScaredRemainingTime > 0) MuteOthers(scaredTrack);
        else if (levelState.AnyGhostsDead) MuteOthers(ghostDownTrack);
        else MuteOthers(normalTrack);
    }

    private void GhostRecovered()
    {
        if (levelState.AnyGhostsDead) MuteOthers(ghostDownTrack);
        else MuteOthers(normalTrack);
    }

    private void GhostElim(GameObject ghost)
    {
        if (levelState.GhostScaredRemainingTime > 0)
        {
            MuteOthers(ghostDownScaredTrack);
        }
        else
        {
            MuteOthers(ghostDownTrack);
        }
    }

    private void GhostScared()
    {
        if (levelState.AnyGhostsDead) MuteOthers(ghostDownScaredTrack);
        else MuteOthers(scaredTrack);
    }
    
    

    private void OnDestroy()
    {
       StopAllCoroutines(); 
       levelState.OnGhostScared -= GhostScared;
       levelState.OnGhostEliminated -= GhostElim;
       levelState.OnGhostRecovered -= GhostRecovered;
       levelState.OnGhostRevived -= GhostRevived;
    }

    void MuteOthers(AudioSource keepTrack)
    {
        foreach (var track in _tracks)
        {
            if (track != keepTrack) track.volume = 0;
        }
        
        keepTrack.volume = 1;
    }

    IEnumerator WaitForIntro()
    {
        introTrack.Play();
        yield return new WaitUntil(() => introTrack.isPlaying);
        yield return new WaitWhile(() => introTrack.isPlaying);

        foreach (var track in _tracks)
        {
            // ensure synced
            track.PlayScheduled(Time.time + 3);
        }
       
        StopCoroutine(nameof(WaitForIntro));
    }

    public bool IntroPlaying =>
        introTrack && introTrack.isPlaying;
}
