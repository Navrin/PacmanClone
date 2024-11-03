using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using Debug = System.Diagnostics.Debug;
using Random = UnityEngine.Random;

public class CherryController : MonoBehaviour
{
    public GameObject CherryPrefab;

    protected GameObject currentCherry;

    LevelStateManager manager;
    private BoundsInt bounds;

    private Camera _cam;
    private Tilemap tilemap;
    private Bounds _camBounds;
    private bool _active = false;

    // Start is called before the first frame update
    public void Ready()
    {
        manager = GetComponent<LevelStateManager>();
        tilemap = manager.tilemap;
        manager.OnGameRestart += Reset;
        _active = true;
        bounds = tilemap.cellBounds;
        _cam = Camera.main;
        _camBounds = new Bounds();

        Debug.Assert(_cam != null, nameof(_cam) + " != null");
        _camBounds.SetMinMax(
            _cam.ScreenToWorldPoint(Vector2.zero),
            _cam.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height))
        );
        
        _camBounds.Expand(3);

        StartCoroutine(nameof(SpawnCherry));
    }

    public void Reset()
    {
        _active = false;
        StopCoroutine(nameof(SpawnCherry));
        DestroyCherry();
        _active = true; 
        StartCoroutine(nameof(SpawnCherry));
    }

    public void OnDisable()
    {
        StopAllCoroutines();
        manager.OnGameRestart -= Reset;
    }

    private Vector2 RandomBoundedPoint()
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
            if (!_active) break;
            yield return new WaitUntil(() => manager.GameActive);
            yield return new WaitForSeconds(10f);
            var spawn = RandomBoundedPoint();
            currentCherry = Instantiate(CherryPrefab, spawn, Quaternion.identity);
            var tweener = currentCherry.GetComponent<MoveTweener>();

            var antiSpawn = new Vector2(-spawn.x, -spawn.y);
            // Debug.Log($"{spawn} -> {antiSpawn}, {tweener.TweenComplete()}");
            tweener.RequestMove(antiSpawn, 15f);
            
            
            yield return new WaitUntil(() => currentCherry == null || tweener.TweenComplete());

            if (currentCherry is not null)
            {
                Destroy(currentCherry);
            }
        }
        yield return null;
    }

    public void DestroyCherry()
    {
        if (currentCherry is not null)
        {
            Destroy(currentCherry);
        }
    }
}
