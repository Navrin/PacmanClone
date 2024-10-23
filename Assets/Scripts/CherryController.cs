using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class CherryController : MonoBehaviour
{
    public GameObject CherryPrefab;

    protected GameObject currentCherry;

    LevelStateManager manager;
    private BoundsInt bounds;

    private Camera _cam;
    private Tilemap tilemap;
    private Bounds _camBounds;

    // Start is called before the first frame update
    void Start()
    {
        manager ??= GetComponent<LevelStateManager>();
        tilemap = manager.tilemap;
        
        bounds = tilemap.cellBounds;
        _cam = Camera.main;
        _camBounds = new Bounds();
        
        _camBounds.SetMinMax(
            _cam.ScreenToWorldPoint(Vector2.zero),
            _cam.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height))
        );
        
        _camBounds.Expand(3);

        StartCoroutine(nameof(SpawnCherry));

    }

    Vector2 RandomBoundedPoint()
    {
        var x = Random.Range(bounds.min.x, bounds.max.x);
        var y  = Random.Range(bounds.min.y, bounds.max.y);

        Vector2 point = tilemap.CellToWorld(new Vector3Int(x, y, 0));
        point *= _camBounds.extents;
        var bounded = _camBounds.ClosestPoint(point);

        return bounded;
    }

    IEnumerator SpawnCherry()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            var spawn = RandomBoundedPoint();
            currentCherry = Instantiate(CherryPrefab, spawn, Quaternion.identity);
            var tweener = currentCherry.GetComponent<MoveTweener>();

            var antiSpawn = new Vector2(-spawn.x, -spawn.y);
            Debug.Log($"{spawn} -> {antiSpawn}, {tweener.TweenComplete()}");
            tweener.RequestMove(antiSpawn, 15f);
            
            
            yield return new WaitUntil(() => currentCherry is null || tweener.TweenComplete());

            if (currentCherry is not null)
            {
                Destroy(currentCherry);
            }
        }
    }

    public void DestroyCherry()
    {
        if (currentCherry is not null)
        {
            Destroy(currentCherry);
        }
    }
}
