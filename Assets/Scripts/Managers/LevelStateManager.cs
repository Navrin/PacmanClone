using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class LevelStateManager : MonoBehaviour
{
    public  GameObject PacStudent;

    public Vector3 spawnPos;

    private AudioManager musicController;
    
    private PacStudentController pacController;
    private CherryController cherryController;
    internal Tilemap tilemap;

    private bool _active = false;

    public delegate void OnGameActiveEvent();
    public event OnGameActiveEvent OnGameActive;
    
    void SyncLevelState()
    {
        tilemap = GameObject.FindWithTag("Tilemap").GetComponent<Tilemap>();
        musicController ??= GameObject.FindWithTag("DynamicMusic").GetComponent<AudioManager>();
        cherryController ??= GetComponent<CherryController>();
        StartCoroutine(nameof(IntroStateWatcher));
        
        var pac = Instantiate(PacStudent, spawnPos, Quaternion.identity);
        pacController = pac.GetComponent<PacStudentController>();
        pacController.levelState = this;
        pacController.OnSpawn();
        
        // pacController ??= GameObject.FindWithTag("Player").GetComponent<PacStudentController>();
        // yield return new WaitUntil(() => pacController.levelGenerator is not null);
        // if (pacController is null)
        // {
            // Debug.Log($"[ERROR] GameObject for PacStudent was not found!");
            // yield return null;
        // }
        

        _active = true;
        
        pacController.OnPacPickup += OnPacCollectItem;
        pacController.OnPacCherryCollide += OnCherryCollide;
        cherryController ??= GetComponent<CherryController>();

        // StopCoroutine(nameof(SyncLevelState));
    }

    private void OnCherryCollide()
    {
       // TODO add score
       cherryController.DestroyCherry();

    }

    private void OnPacCollectItem(Vector3Int pos, int kind)
    {
        var kindName = kind == TileType.Pellet ? "Pellet" : "Powerup";
        // Debug.Log($"Pac picking up item at {pos}, kind {kindName}");
        
        // todo handle powerups
        if (kind == TileType.Pellet)
        {
            // todo add score 
            tilemap.SetTile(pos, null);
        }
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoad;
        
    }

    private void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        // TODO: don't use build index hardcoded values
        if (scene.buildIndex == 1) SyncLevelState();
    }
    
    IEnumerator IntroStateWatcher()
    {
        yield return new WaitUntil(() => musicController.introTrack.isPlaying);
        yield return new WaitWhile(() => musicController.introTrack.isPlaying);
        OnGameActive?.Invoke();
        cherryController.enabled=true;
        StopCoroutine(nameof(IntroStateWatcher));
    }
}
