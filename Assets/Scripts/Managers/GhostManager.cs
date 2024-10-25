using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostManager : MonoBehaviour
{
    public LevelStateManager levelState;
    public GameObject ghostPrefab;

    public GhostController[] activeGhosts;
    public GhostProperties props;
    
    void Start()
    {
        levelState ??= GetComponent<LevelStateManager>();


        levelState.OnGameLoaded += OnGameLoaded;
    }

    private void OnGameLoaded()
    {
        SpawnGhosts();
        
    }

    private void SpawnGhosts()
    {
        activeGhosts = new GhostController[4];
        for (var i = 0; i < activeGhosts.Length; i++)
        {
            var spawnPoint = props.spawnPoints[i];

            var onMap = levelState.tilemap.GetCellCenterWorld(spawnPoint);
            
            var ghost = Instantiate(ghostPrefab,onMap, Quaternion.identity);
            var controller = ghost.GetComponent<GhostController>();
            controller.ghostIdentifier = i;
            controller.managers = gameObject;
            controller.Spawn();
            
            activeGhosts[i] = controller;
        }
    }
}
