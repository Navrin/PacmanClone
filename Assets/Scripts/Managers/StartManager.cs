using UnityEngine;
using UnityEngine.SceneManagement;

public class StartManager : MonoBehaviour
{
    public static GameObject instance;
    public enum Levels
    {
        Level1 =1, Level2=2
    }
    // Start is called before the first frame update
    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        if (instance == null)
        {
            instance = gameObject;
        }
        else if (this.gameObject != instance)
        {
            Destroy(this.gameObject);
        }
    }

    public void OnLevelButton(int level)
    {
        // todo make loading screens
        SceneManager.LoadScene(level);
    }
    
}
