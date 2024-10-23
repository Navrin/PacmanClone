using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartManager : MonoBehaviour
{
    public enum Levels
    {
        Level1 =1, Level2=2
    }
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        
    }

    public void OnLevelButton(int level)
    {
        // todo make loading screens
        SceneManager.LoadScene((int)level);
    }
    
}
