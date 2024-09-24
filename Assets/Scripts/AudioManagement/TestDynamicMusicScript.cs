using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDynamicMusicScript : MonoBehaviour
{
    public AudioSource normalTrack;
    public AudioSource scaredTrack;
    public AudioSource ghostDownTrack;
    public AudioSource ghostDownScaredTrack;
    List<AudioSource> _tracks = new List<AudioSource>();
    
    // Start is called before the first frame update
    void Start()
    {
        _tracks.Add(normalTrack);
        _tracks.Add(scaredTrack);
        _tracks.Add(ghostDownTrack);
        _tracks.Add(ghostDownScaredTrack);
    }

    void MuteOthers(AudioSource keepTrack)
    {
        foreach (var track in _tracks)
        {
            if (track != keepTrack) track.volume = 0;
        }
    }

    void Update()
    { 
        AudioSource track = null;
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            track = normalTrack;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            track = scaredTrack;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            track = ghostDownTrack;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            track = ghostDownScaredTrack;
        }

        if (track is not null)
        {
            MuteOthers(track);
            track.volume = 1;
        }
        
    }
}
