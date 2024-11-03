using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartScreenController : MonoBehaviour
{
    public Button level1Button;
    public Button level2Button;

    public TMP_Text highScore;
    public TMP_Text bestTime;

    public GameObject manager;
    // Start is called before the first frame update
    void Start()
    {
        manager ??= GameObject.Find("Manager");
        var start = manager.GetComponent<StartManager>();
        
        level1Button.onClick.AddListener(() => start.OnLevelButton(1));

        var highscore = GameScoreState.LoadHighScore();

        highScore.text = $"Best Score: {highscore.Score}";
        bestTime.text = $"Best Time: {TimeSpan.FromSeconds(highscore.Time).ToString(HUDStateController.GameTimeFormat)}";
    }

    // Update is called once per frame
}
