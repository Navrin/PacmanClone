using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class PacStudentController : MonoBehaviour, MainControls.IGameActions
{
    
    public LevelGenerator levelGenerator;
    public MoveTweener moveTweener;
    // public LevelBeginManager levelBeginManager;
    public LevelStateManager levelState;

    /**
     * we need to reference Pac's position in the cell and
     * not the current transform position, otherwise we might
     * request movements that will move into the very corner of a wall.
     * we should update the position at the end of every tween.
     */
    [field: SerializeField]
    internal Vector3Int PacPosition;

    public bool GameReady = false;
    
    private Tilemap _tilemap;
    private KeyCode _lastInput;
    private Direction? _lastValidDirection;
    private Direction? _lastDirection = null;
    private int _targetTileType = -1;
    private bool _firstMove;
    private bool _isMoving = false;

    public Direction LastValidDirection => _lastValidDirection.GetValueOrDefault(Direction.East);
    
    public PlayerInput input;
    private MainControls _controls;
    
    public delegate void OnPacPickupEvent(Vector3Int pos, int kind);
    public event OnPacPickupEvent OnPacPickup;

    public delegate void OnPacCollisionEvent(Vector3Int pos);
    public event OnPacCollisionEvent OnPacCollision;

    public delegate void OnPacMoveStartEvent(Vector3Int pos);
    public event OnPacMoveStartEvent OnPacMoveStart;

    public delegate void OnPacMoveEmptyEvent(Vector3Int pos);
    public event OnPacMoveEmptyEvent OnPacMoveEmpty;

    public delegate void OnPacAnyMoveEvent(Vector3Int pos);
    public event OnPacAnyMoveEvent OnPacAnyMove;

    public delegate void OnPacCherryCollideEvent();
    public event OnPacCherryCollideEvent OnPacCherryCollide;

    public delegate void OnGhostCollideEvent(GameObject ghost);
    public event OnGhostCollideEvent OnGhostCollide;

    public delegate void OnPacDeathEvent();
    public event OnPacDeathEvent OnPacDeath;

    public delegate void OnPacResetEvent();
    public event OnPacResetEvent OnPacReset;
    
    public KeyCode LastInput
    {
        get => _lastInput;
        set {
           var dir = KeyToDirection(value);
           _lastInput = value;
           _lastDirection = dir; 
           if (moveTweener.TweenComplete()) 
               HandleNextValidDir();
        }
    }
    
    Direction KeyToDirection(KeyCode key) {
            return key switch
            {
                KeyCode.UpArrow or KeyCode.W => Direction.North,
                KeyCode.LeftArrow or KeyCode.A => Direction.West,
                KeyCode.RightArrow or KeyCode.D => Direction.East,
                KeyCode.DownArrow or KeyCode.S => Direction.South,
                _ => Direction.East
            };
    }

    private readonly int solidWallMask =
        (1 << TileType.OutsideCorner
        | 1 << TileType.OutsideWall
        | 1 << TileType.InsideCorner
        | 1 << TileType.InsideWall);

    private readonly int pickupMask =
        (1 << TileType.Pellet
         | 1 << TileType.PowerUp);

    public void OnSpawn()
    {
        levelGenerator ??= GameObject.FindWithTag("LevelGenerator").GetComponent<LevelGenerator>();
        moveTweener ??= GetComponent<MoveTweener>();
        
        _tilemap = levelGenerator.tilemap;

        OnRespawn();
        levelState.OnGameActive += OnGameStart;
        
        moveTweener.OnTweenComplete += OnPacMoveComplete;
        moveTweener.OnTweenHalfComplete += OnTweenMidpoint;
        levelState.OnLifeChange += PacDeathEvent;
        levelState.OnGameExit += OnGameExit;
        levelState.OnGameRestart += GameRestart;
    }

    private void GameRestart()
    {
        OnRespawn();
    }

    private void PacDeathEvent(int lives)
    {
        OnPacDeath?.Invoke();
        moveTweener.ForceStop();
        GameReady = false;
        
    }

    public void OnRespawn()
    {
        _lastValidDirection = null;
        OnPacAnyMove?.Invoke(PacPosition);

        _controls?.Game.Disable();
        LastInput = KeyCode.D;
        _isMoving = false;
        SnapToGrid(gameObject.transform);
        OnPacReset?.Invoke();
        
    }

    private void OnGameStart()
    {
        _controls?.Game.Enable();
        LastInput = KeyCode.D;
        GameReady = true;
    }

    private void OnGameExit()
    {
        _controls?.Game.Disable();
        GameReady = false;
        
    }


    public void SnapToGrid(Transform transform)
    {
        var pos = _tilemap.WorldToCell(transform.position);
        var snapped = _tilemap.GetCellCenterWorld(pos);
        transform.position = snapped;
        // PacPosition = _tilemap.WorldToCell(transform.position);

    }
    public void OnEnable()
    {
        if (_controls == null)
        {
            _controls = new MainControls();
            _controls.Game.SetCallbacks(this);
        }
        // _controls.Game.Enable();
    }

    public void OnDisable()
    {
        _controls.Game.Disable();
    }
    // Update is called once per frame
    void Update()
    {
        // We are going to use the new input system because 
        // the old input system is depreciated, and we can add controller support.

        // TODO add code here to handle pac collision no movement event.
        // if (moveTweener.TweenComplete() && _lastDirection.HasValue)
        // {
        //     HandleNextPacMove(_lastDirection.GetValueOrDefault(Direction.East));
        // }
        
        PacPosition = PositionInTile(transform);
        if (!GameReady) return;
        if (!_isMoving)
        {
            HandleNextPacMove(_lastValidDirection.GetValueOrDefault(Direction.East));
        }
    }

    void HandleNextPacMove(Direction direction)
    {
        var tile = GetNextTile(PacPosition, direction); 
        
        if (moveTweener.TweenActive()) return;
        if (CheckInvalidMove(tile))
        {
            // collision occured
            if (
                // PacPosition != _collisionInvokedAt
                _isMoving
                && moveTweener.TweenComplete())
            {
                // _collisionInvokedAt = PacPosition;
                _isMoving = false;
                OnPacCollision?.Invoke(PacPosition);
            }
            
            return;
        };

        _targetTileType = tile;
        // moveTweener.RequestMove(direction);
        // ensuring grid snap
        // _collisionInvokedAt = Vector3Int.one;
        if (!_isMoving)
        {
            OnPacMoveStart?.Invoke(PacPosition);
            _isMoving = true;
        }
        OnPacAnyMove?.Invoke(PacPosition);
        MovePacToDir(direction);
    }

    private void MovePacToDir(Direction direction)
    {
        var dirVec = direction.ToVec();
        var intPos = PacPosition + new Vector3Int((int)dirVec.x, (int)dirVec.y, 0);
        var outsideLeftBounds = intPos.x < _tilemap.cellBounds.xMin;
        if (outsideLeftBounds || intPos.x >= _tilemap.cellBounds.xMax)
        {
            PacPosition = new Vector3Int(
                outsideLeftBounds ? _tilemap.cellBounds.xMax - 1 : _tilemap.cellBounds.xMin + 1, 
                intPos.y,
                0
            );
            transform.position = _tilemap.GetCellCenterWorld(PacPosition);
            
            intPos = PacPosition + new Vector3Int((int)dirVec.x, (int)dirVec.y, 0); 
        }
        
        var worldPos = _tilemap.GetCellCenterWorld(intPos);
        moveTweener.RequestMove(worldPos);
    }

    private void OnPacMoveComplete()
    {
        // PacPosition = PositionInTile(transform);
        // if (_lastValidDirection.HasValue)
        //     PacPosition = AddTilemapPos(PacPosition, _lastValidDirection.Value);
        HandleNextValidDir();
        
        if (_lastValidDirection.HasValue)
        {
            HandleNextPacMove(_lastValidDirection.Value);
        }
    }

    private void HandleNextValidDir()
    {
        if (!_lastDirection.HasValue) return;
        if (CheckInvalidMove(_lastDirection.Value)) return;
        
        _lastValidDirection = _lastDirection;
    }

    private void OnTweenMidpoint()
    {
        
        if (CheckMask(pickupMask, _targetTileType) && _lastValidDirection.HasValue)
        {
            OnPacPickup?.Invoke(
                AddTilemapPos(PacPosition, _lastValidDirection.Value),
                _targetTileType
            );
        }
        Debug.Log(_targetTileType);
        if (_targetTileType == -1)
        {
            Debug.Log($"Invoking empty move event");
            OnPacMoveEmpty?.Invoke(AddTilemapPos(PacPosition, _lastValidDirection.GetValueOrDefault(Direction.East)));
        }
    }

    private static bool CheckMask(int mask, int tile)
    {
        return (mask & (1 << tile)) > 0;
    }
    
    private bool CheckInvalidMove(int tile)
    {
        return CheckMask(solidWallMask, tile);
    }
    
    private bool CheckInvalidMove(Direction direction)
    {
        var tile = GetNextTile(PacPosition, direction);
        // Debug.Log($"Tile {tile} for {direction} -> {solidWallMask} & {1 << tile}");
        return CheckInvalidMove(tile);
    }

    int GetNextTile(Vector3Int pos, Direction dir)
    {
        // var pos = PositionInTile(trans);
        var newPos = AddTilemapPos(pos, dir);


        var tile = levelGenerator.tileset.TileToMap(_tilemap.GetTile(newPos));
        // Debug.Log($"Current pos {pos} to {newPos} = {tile}");
        return tile;
    }

    private static Vector3Int AddTilemapPos(Vector3Int pos, Direction dir)
    {
        var dirVec = dir.ToVec();
        return new Vector3Int(pos.x + (int)dirVec.x, pos.y + (int)dirVec.y, 0);
    }

    Vector3Int PositionInTile(Transform trans)
    {
        return _tilemap.WorldToCell(trans.position);
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
        var movement = context.ReadValue<Vector2>();
        LastInput = movement.x switch
        {
            > 0 => KeyCode.D,
            < 0 => KeyCode.A,
            _ => movement.y switch
            {
                > 0 => KeyCode.W,
                < 0 => KeyCode.S,
                _ => LastInput
            }
        };
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!GameReady) return;
        
        Debug.Log($"Collision event: {other.name}");
        if (other.CompareTag("Cherry"))
        {
            OnPacCherryCollide?.Invoke();
        } else if (other.CompareTag("Ghost"))
        {
            OnGhostCollide?.Invoke(other.gameObject);
        }
    }
}
