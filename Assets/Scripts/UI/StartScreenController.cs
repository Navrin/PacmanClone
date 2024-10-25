using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartScreenController : MonoBehaviour
{
    public Button level1Button;
    public Button level2Button;

    public GameObject manager;
    // Start is called before the first frame update
    void Start()
    {
        manager ??= GameObject.Find("Manager");
        var start = manager.GetComponent<StartManager>();
        
        level1Button.onClick.AddListener(() => start.OnLevelButton(1));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
