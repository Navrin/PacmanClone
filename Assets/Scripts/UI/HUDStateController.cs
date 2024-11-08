using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDStateController : MonoBehaviour
{
    private static readonly string GhostTimerTemplate = "{0} sec{1} <sprite anim=\"10,11,12\">";
    private static readonly string GameTimeMonoTemplate = "<mspace=25>{0}";
    public static readonly string GameTimeFormat = "mm':'ss':'ff";
    private static readonly string GameTimeFormatBlink = "mm' 'ss' 'ff";
    
    public GameObject heartContainer;
    private Transform[] _hearts;
    public GameObject scoreContainer;
    private TMP_Text _scoreText;
    private TMP_Text _scoreAddText;
    
    public GameObject ghostTimer;
    private TMP_Text _ghostTimerText;
    
    public GameObject gameTime;
    private TMP_Text _gameTimeText;

    public Button exitGame;

    private LevelStateManager _stateManager;
    public TMP_Text roundStartText;

    private bool _setupError = false;

    private int _targetScore = 0;
    private int _displayedScore = 0;
    private int _scoreIncrease = 0;
    private int _incrementBy = 0;
    
    
    private void Awake()
    {
        var scoreContainerChildren = scoreContainer.GetComponentsInChildren<TMP_Text>();
        foreach (var tmp in scoreContainerChildren)
        {
            if (tmp.name.Contains("Add"))
            {
                _scoreAddText = tmp; 
            }
            else
            {
                _scoreText = tmp;
            }
        }

        if (scoreContainerChildren.Length == 0)
        {
            Debug.LogError("Score container is empty");
            _setupError = true;
        }
        _ghostTimerText = ghostTimer.GetComponent<TMP_Text>();
        if (_ghostTimerText is null)
        {
           Debug.LogError("GhostTimerText is null!");
           _setupError = true;

        }
        _gameTimeText = gameTime.GetComponent<TMP_Text>();
        if (_gameTimeText is null)
        {
            Debug.LogError("Game time not found");
            _setupError = true;

        }
        
        var heartCount = heartContainer.transform.childCount;
        _hearts = new Transform[heartCount];
        for (var i = 0; i < heartCount; i++)
        {
            _hearts[i]=heartContainer.transform.GetChild(i);
        }
        if (_hearts is null)
        {
            Debug.LogError("Hearts not found");
            _setupError = true;

        }
        
        var managers = GameObject.FindWithTag("Managers");
        managers.TryGetComponent(out _stateManager);
        if (_stateManager is null)
        {
            Debug.LogError("Manager not found");
            _setupError = true;
            return; 
        }
        
        _stateManager.OnGameLoaded += OnGameLoad;
        _stateManager.OnGhostScared += OnGhostScared;
        _stateManager.OnLifeChange += OnLifeChange;
        _stateManager.OnScoreChange += OnScoreChange;
        _stateManager.OnGameOver += GameOverScreen;
        
        exitGame.onClick.AddListener(ExitGame);

    }
    
    private void OnDestroy()
    {
        StopAllCoroutines();
        _stateManager.OnGameLoaded -= OnGameLoad;
        _stateManager.OnGhostScared -= OnGhostScared;
        _stateManager.OnLifeChange -= OnLifeChange;
        _stateManager.OnScoreChange -= OnScoreChange;
        _stateManager.OnGameOver -= GameOverScreen;
    }

    private void GameOverScreen()
    {
        roundStartText.gameObject.SetActive(true);
        roundStartText.text = "GAME OVER.";
    }

    private void OnGameLoad()
    {
        StartCoroutine(RoundStartAnim());
    }



    private void ExitGame()
    {
        if (_setupError) return;
        _stateManager.RequestExitGame();
    }
    private void OnScoreChange(int score, int amount)
    {
        _targetScore = score;
        _scoreIncrease = amount;
        _incrementBy = amount / 4;

        _scoreAddText.text = $"+{_scoreIncrease}";
    }

    private void OnLifeChange(int livesLeft)
    {
        var livesHidden = _hearts.Length - livesLeft;
        
        for (var i = 0; i < livesHidden; i++)
        {
            _hearts[_hearts.Length - 1 - i].gameObject.SetActive(false);
        }
    }

    private void OnGhostScared()
    {
        _ghostTimerText.text = string.Format(GhostTimerTemplate, LevelStateManager.GhostScaredTotalTime, "s");
        ghostTimer.SetActive(true);
    }

    private void LateUpdate()
    {
        if (_setupError) return;

        if (_stateManager.GameActive && _stateManager.scoreState is not null)
        {
            var span = _stateManager.scoreState.GameTime(Time.time);
            var fmt = span.Seconds % 2 == 0 ? GameTimeFormat : GameTimeFormatBlink;
            var timeFormatted = span.ToString(fmt);

            _gameTimeText.text = string.Format(GameTimeMonoTemplate, timeFormatted);
        }
        else
        {
            _gameTimeText.text = "00:00:00";
        }

        if (_displayedScore != _targetScore && Time.frameCount % 15 == 0)
        {
            _displayedScore += _incrementBy;
            _scoreText.text = _displayedScore.ToString();
            _scoreIncrease -= _incrementBy;
            _scoreAddText.text = $"+{_scoreIncrease}";
        }

        if (_scoreIncrease <= 0)
        {
            _incrementBy = 0;
            _displayedScore = _targetScore;
            _scoreAddText.text = "";
        }

        var scaredTimer = _stateManager.GhostScaredRemainingTime;
        if (scaredTimer > 0 && Time.frameCount % 30 == 0)
        {
            _ghostTimerText.text = string.Format(GhostTimerTemplate, scaredTimer, scaredTimer == 1 ? "" : "s");
        } else if (scaredTimer <= 0)
        {
            ghostTimer.SetActive(false);
        }
    }


    IEnumerator RoundStartAnim()
    {
        roundStartText.gameObject.SetActive(true);
        for (var i = 3; i > 0; i--)
        {
            roundStartText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        roundStartText.text = "GO!!!";
        yield return new WaitForSeconds(1f);
        roundStartText.gameObject.SetActive(false);
        roundStartText.text = "3";
    }
}
