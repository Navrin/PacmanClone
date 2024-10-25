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
        levelState.OnLifeChange += HaltGhosts;
        levelState.OnGhostEliminated += GhostElim;
        levelState.OnGhostRevived += GhostRevive;
        levelState.OnGameRestart += OnRestart;
    }

    private void OnRestart()
    {
        foreach (var g in activeGhosts)
        {
            Destroy(g.gameObject);
        }
    }

    private void HaltGhosts(int lives)
    {
        // TODO: stop ai behaviour
    }

    private void GhostRevive(GameObject ghost)
    {
        foreach (var g in activeGhosts)
        {
            if (g.gameObject == ghost)
            {
                g.GhostRevive();
            }
        }
    }

    private void GhostElim(GameObject ghost)
    {
        foreach (var g in activeGhosts)
        {
            if (g.gameObject == ghost)
            {
                g.GhostDead();
            }
        }
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
