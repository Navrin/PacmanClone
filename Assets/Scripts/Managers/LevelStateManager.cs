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
    GhostElim = 300,
}

[Serializable]
public class HighScore
{
    [SerializeField] public int Score = 0;
    [SerializeField] public float Time = 0;

    public HighScore(int score, float time)
    {
        this.Score = score;
        this.Time = time;
    }
}
// [Serializable]
public class GameScoreState
{
    public int Score { get; set; }
    public int Lives { get; private set; }
    
    // TODO: change this to allow for 
    // pausing, i.e in-between lives
    // maybe use a timespan and "last active time" 
    private float _accumulatedTime = 0.0f;
        
    // public float StartTime { get; set; }
    private float _lastTimeStamp = -1;
    private bool _timerPaused = false;
    
    public TimeSpan GameTime(float current)
    {
        if (_timerPaused) return TimeSpan.FromSeconds(_accumulatedTime);
        var span = TimeSpan.FromSeconds((current-_lastTimeStamp) + _accumulatedTime);
        return span;
    }

    public void PauseTimer(float time)
    {
        
        var span = GameTime(time);
        Debug.Log($"AccTime {_accumulatedTime} ");
        Debug.Log($"{span}");

        _timerPaused = true;
        _accumulatedTime += (float) span.TotalSeconds;
        _lastTimeStamp = time;
    }

    public void ResumeTimer(float time)
    {
        Debug.Log($"AccTime {_accumulatedTime} ");
        _timerPaused = false; 
        _lastTimeStamp = time;
    }
    
    public GameScoreState(float time)
    {
        this.Score = 0;
        this.Lives = 3;
        _lastTimeStamp = time;
    }

    public void ChangeLives(bool death)
    {
        Debug.Log($"CHANGING LIFE AMOUNT PRIOR {Lives}");
        Lives += death ? -1 : 1;
    }
    public void AddScore(ScoringEvent scoreEvent)
    {
        Score += (int)scoreEvent;
    }

    public void Save(float time)
    {
        var best = LoadHighScore();
        if (best.Score <= Score && best.Time < GameTime(time).TotalSeconds)
        {
            var newScore = new HighScore(Score, (float)GameTime(time).TotalSeconds);
            var bestScore = JsonUtility.ToJson(newScore);
            PlayerPrefs.SetString("HighScore", bestScore);
        }
    }

    static public HighScore LoadHighScore()
    {
        var highscore = PlayerPrefs.GetString("HighScore", "");
        if (highscore == "") return new HighScore(0, 0);
        
        return JsonUtility.FromJson<HighScore>(highscore);
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

    public GameScoreState scoreState = null;

    private bool _active = false;
    public float StartTime { get; private set; }
    public bool GameActive { get; private set; }
    public float GhostScaredStart { get; private set; }

    public int GhostScaredRemainingTime => GhostScaredTotalTime - Mathf.FloorToInt(Time.time - GhostScaredStart);
        
    public static readonly int GhostScaredTotalTime = 10;
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

    public delegate void OnGhostEliminatedEvent(GameObject ghost);
    public event OnGhostEliminatedEvent OnGhostEliminated;
    public delegate void OnGhostRecoveredEvent();
    public event OnGhostRecoveredEvent OnGhostRecovered;
    public delegate void OnGhostRevivedEvent(GameObject ghost);
    public event OnGhostRevivedEvent OnGhostRevived;
    

    public delegate void OnGameExitEvent();
    public event OnGameExitEvent OnGameExit;
    
    /**
     * called when the level is first loaded in, before the ready countdown
     */
    public delegate void OnGameLoadedEvent();
    public event OnGameLoadedEvent OnGameLoaded;

    public delegate void OnGameRestartEvent();
    public event OnGameRestartEvent OnGameRestart;


    public List<GameObject> deadGhosts = new List<GameObject>();
    public bool AnyGhostsDead => deadGhosts.Count > 0;

    private int _pelletsLeft = 0;
    private int _powerupsLeft = 0;
    
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
        pacController.OnPacPickup += OnPacCollectItem;
        pacController.OnPacCherryCollide += OnCherryCollide;
        pacController.OnGhostCollide += OnGhostCollide;
        _powerupsLeft = pacController.levelGenerator.powerupCount;
        _pelletsLeft = pacController.levelGenerator.pelletCount;
        
        _active = true;
        cherryController.Ready();

        // StopCoroutine(nameof(SyncLevelState));
    }


    private bool handlingDeath = false;
    private void OnGhostCollide(GameObject ghost)
    {
        if (handlingDeath) return;
        
        if (GhostScaredRemainingTime > 0)
        {
            // TODO: inform ghost controller of ghost death
            AddScore(ScoringEvent.GhostElim);
            OnGhostEliminated?.Invoke(ghost);
            deadGhosts.Add(ghost);
            StartCoroutine(GhostReviveTimer(ghost));
        }
        else if (!deadGhosts.Contains(ghost))
        {
            // TODO: death event
            handlingDeath = true;
            StartCoroutine(PacDeath());
        }
    }

    IEnumerator PacDeath()
    {
        scoreState.ChangeLives(true);
        OnLifeChange?.Invoke(scoreState.Lives);
        

        // TODO: PAUSE TIMER
        scoreState.PauseTimer(Time.time);

        if (scoreState.Lives == 0)
        {
            yield return GameOver();
        }
        
        yield return new WaitForSeconds(3);
        // RespawnPac();
        pacController.transform.position = spawnPos;
        OnGameRestart?.Invoke();
        StartCoroutine(IntroStateWatcher());

    }

    IEnumerator  GameOver()
    {
        OnGameOver?.Invoke();
        scoreState.Save(Time.time);
            
        yield return new WaitForSeconds(3);
        RequestExitGame();
    }

    IEnumerator GhostReviveTimer(GameObject ghost)
    {
        yield return new WaitForSeconds(5);
        deadGhosts.Remove(ghost);
        OnGhostRevived?.Invoke(ghost);
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
            _pelletsLeft--;
        }

        if (kind == TileType.PowerUp)
        {
            tilemap.SetTile(pos, null);
            PowerUpCollected();
            _powerupsLeft--;
        }

        if (_pelletsLeft == 0 && _powerupsLeft == 0)
        {
            StartCoroutine(GameOver());
        }
    }

    private void PowerUpCollected()
    {
        OnGhostScared?.Invoke();
        GhostScaredStart = Time.time;

        StartCoroutine(nameof(PowerupTimer));
    }

    IEnumerator PowerupTimer()
    {
        yield return new WaitForSeconds(GhostScaredTotalTime);
        OnGhostRecovered?.Invoke();
        StopCoroutine(nameof(PowerupTimer));
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

        yield return new WaitForSeconds(3 - musicController.introTrack.clip.length);
         
        StartGameActive();
    }

    private void StartGameActive()
    {
        handlingDeath = false;
        OnGameActive?.Invoke();
        cherryController.enabled=true;
        // StartTime = Time.time;
        
        GameActive = true;

        scoreState ??= new GameScoreState(Time.time);
        scoreState.ResumeTimer(Time.time);
    }

    private void EndGameInactive()
    {
        StartTime = -1;
        OnGameExit?.Invoke();
        cherryController.enabled=false;
        // scoreState.EndTime = Time.time;
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
