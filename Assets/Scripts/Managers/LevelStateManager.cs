using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[Serializable]
public enum ScoringEvent
{
    Pellet = 10,
    Cherry = 100,
    GhostElim = 50,
}

[Serializable]
public class GameScoreState
{
    public int Score { get; set; }
    public int Lives { get; set; }
    
    // TODO: change this to allow for 
    // pausing, i.e in-between lives
    // maybe use a timespan and "last active time" 
    public float StartTime { get; set; }
    public float EndTime { get; set; }
    
    public GameScoreState(float time)
    {
        Score = 0;
        Lives = 3;
        StartTime = time;
    }

    public void AddScore(ScoringEvent scoreEvent)
    {
        Score += (int)scoreEvent;
    }
}

public class LevelStateManager : MonoBehaviour
{
    public  GameObject PacStudent;

    public Vector3 spawnPos;

    private AudioManager musicController;
    
    private PacStudentController pacController;
    private CherryController cherryController;
    internal Tilemap tilemap;

    public GameScoreState scoreState;

    private bool _active = false;
    public float StartTime { get; private set; }
    public bool GameActive { get; private set; }
    public float GhostScaredStart { get; private set; }

    public int GhostScaredRemainingTime => GhostScaredTotalTime - Mathf.FloorToInt(Time.time - GhostScaredStart);
        
    public static readonly int GhostScaredTotalTime = 15;
    public delegate void OnGameActiveEvent();
    public event OnGameActiveEvent OnGameActive;
    
    /**
     * TODO: Impl
     */
    public delegate void OnGameOverEvent();
    public event OnGameOverEvent OnGameOver;

    public delegate void OnScoreChangeEvent(int score, int amount);
    public event OnScoreChangeEvent OnScoreChange;

    public delegate void OnLifeChangeEvent(int lives);
    public event OnLifeChangeEvent OnLifeChange;
    
    public delegate void OnGhostScaredEvent();
    public event OnGhostScaredEvent OnGhostScared;

    public delegate void OnGameExitEvent();
    public event OnGameExitEvent OnGameExit;
    
    /**
     * called when the level is first loaded in, before the ready countdown
     */
    public delegate void OnGameLoadedEvent();
    public event OnGameLoadedEvent OnGameLoaded;
    
    
    IEnumerator SyncLevelState()
    {
        if (this == null) yield return null;
        if (this.gameObject != StartManager.instance) yield return null;
        yield return new WaitUntil(() => SceneManager.GetActiveScene().buildIndex == 1);
        
        tilemap = GameObject.FindWithTag("Tilemap").GetComponent<Tilemap>();
        musicController = GameObject.FindWithTag("DynamicMusic").GetComponent<AudioManager>();
        cherryController = GetComponent<CherryController>();
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
        cherryController.Ready();

        // StopCoroutine(nameof(SyncLevelState));
    }

    private void OnCherryCollide()
    {
        cherryController.DestroyCherry();
        AddScore(ScoringEvent.Cherry);
    }

    private void AddScore(ScoringEvent scoreEvent)
    {
        scoreState.AddScore(scoreEvent);
        OnScoreChange?.Invoke(scoreState.Score, (int)scoreEvent);
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
            AddScore(ScoringEvent.Pellet);
        }
    }

    private void Start()
    {
        if (StartManager.instance == null)
            StartManager.instance = this.gameObject;
        else if (this.gameObject != StartManager.instance)
        {
            Destroy(this.gameObject);
            return;
        }
        SceneManager.activeSceneChanged += OnSceneChange;
        
    }

    private void OnSceneChange(Scene scene1, Scene scene2)
    {
        // TODO: don't use build index hardcoded values
        Debug.Log($"Scene changed from {scene1.name} -> {scene2.name}");
        if (scene2.buildIndex == 1) StartCoroutine(SyncLevelState());
    }
    
    IEnumerator IntroStateWatcher()
    {
        OnGameLoaded?.Invoke();
        yield return new WaitUntil(() => musicController is not null && musicController.introTrack);
        yield return new WaitUntil(() => musicController is not null && musicController.IntroPlaying);
        yield return new WaitWhile(() => musicController is not null && musicController.IntroPlaying);
         
        StartGameActive();
        StopCoroutine(nameof(IntroStateWatcher));
    }

    private void StartGameActive()
    {
        OnGameActive?.Invoke();
        cherryController.enabled=true;
        StartTime = Time.time;
        GameActive = true;

        scoreState = new GameScoreState(Time.time);
    }

    private void EndGameInactive()
    {
        StartTime = -1;
        OnGameExit?.Invoke();
        cherryController.enabled=false;
        scoreState.EndTime = Time.time;
        StopAllCoroutines();
        // todo : add saving 
        
    }

    public void RequestExitGame()
    {
        EndGameInactive();
        // TODO: impl cleanup
        SceneManager.LoadScene(0);
    }
}
