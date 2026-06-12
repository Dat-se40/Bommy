using PurrNet;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class MovementController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Grid")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap playgroundTilemap;
    [SerializeField] private Tilemap indestructibleTilemap;
    [SerializeField] private Tilemap destructibleTilemap;

    [Header("Obstacle")]
    [SerializeField] private LayerMask bombLayer;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    private Vector3Int currentCell;
    private Vector3Int targetCell;
    private Vector2 targetWorldPosition;
    private bool isMoving;
    private int direction;



    public Vector3Int CurrentCell => currentCell;
    public bool IsMoving => isMoving;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!isOwner) return;
        if (grid == null) return;
        if (!isMoving)
            ReadInput();

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (!isOwner) return;
        MoveToTargetCell();
    }

    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        Vector3Int inputDirection = Vector3Int.zero;

        bool up = keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
        bool down = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
        bool left = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool right = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

        if (left)
        {
            inputDirection = Vector3Int.left;
            direction = 2;
        }
        else if (right)
        {
            inputDirection = Vector3Int.right;
            direction = 3;
        }
        else if (up)
        {
            inputDirection = Vector3Int.up;
            direction = 1;
        }
        else if (down)
        {
            inputDirection = Vector3Int.down;
            direction = 0;
        }

        if (inputDirection == Vector3Int.zero) 
        {
            direction = 0;
            return;
        }
            

        TryStartMove(inputDirection);
    }

    private void TryStartMove(Vector3Int inputDirection)
    {
        Vector3Int nextCell = currentCell + inputDirection;

        if (!CanStandAtCell(nextCell))
            return;

        targetCell = nextCell;
        targetWorldPosition = grid.GetCellCenterWorld(targetCell);
        isMoving = true;
    }

    private void MoveToTargetCell()
    {
        if (!isMoving || rb == null)
            return;

        Vector2 newPosition = Vector2.MoveTowards(
            rb.position,
            targetWorldPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(newPosition);

        float distance = Vector2.Distance(newPosition, targetWorldPosition);

        if (distance <= 0.001f)
        {
            rb.MovePosition(targetWorldPosition);
            SetCurrentCell(targetCell);
            isMoving = false;
        }
    }

    void SetCurrentCell(Vector3Int cell)
    {
        currentCell = cell;
        targetCell = cell;

        if (isOwner && !isServer)
            ReportCurrentCellRpc(cell);
    }

    [ServerRpc]
    void ReportCurrentCellRpc(Vector3Int cell, RPCInfo rpcInfo = default)
    {
        currentCell = cell;
        targetCell = cell;
    }

    public void SnapToNearestValidCell()
    {
        if (grid == null)
        {
            Debug.LogWarning("Grid is not assigned.");
            return;
        }

        Vector3Int startCell = grid.WorldToCell(transform.position);

        if (!CanStandAtCell(startCell))
            startCell = FindNearestValidCell(startCell);

        SetCurrentCell(startCell);

        Vector3 center = grid.GetCellCenterWorld(startCell);

        transform.position = center;

        if (rb != null)
            rb.position = center;

        targetWorldPosition = center;
    }

    private Vector3Int FindNearestValidCell(Vector3Int origin)
    {
        int searchRadius = 8;

        for (int radius = 0; radius <= searchRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector3Int cell = new Vector3Int(origin.x + x, origin.y + y, origin.z);

                    if (CanStandAtCell(cell))
                        return cell;
                }
            }
        }

        Debug.LogWarning("No valid cell found near player. Using original cell.");
        return origin;
    }

    public bool CanStandAtCell(Vector3Int cell)
    {
        if (grid == null)
            return false;

        // Nếu có Playground thì chỉ được đi trên tile Playground.
        if (playgroundTilemap != null && !playgroundTilemap.HasTile(cell))
            return false;

        if (indestructibleTilemap != null && indestructibleTilemap.HasTile(cell))
            return false;

        if (destructibleTilemap != null && destructibleTilemap.HasTile(cell))
            return false;

        Vector3 cellCenter = grid.GetCellCenterWorld(cell);

        Collider2D bombCollider = Physics2D.OverlapBox(
            cellCenter,
            new Vector2(0.75f, 0.75f),
            0f,
            bombLayer
        );

        if (bombCollider != null)
            return false;

        return true;
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetBool("IsMoving", isMoving);
        animator.SetInteger("Direction", direction);
    }

    private void OnDrawGizmosSelected()
    {
        if (grid == null)
            return;

        Vector3Int cell = grid.WorldToCell(transform.position);
        Vector3 center = grid.GetCellCenterWorld(cell);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, Vector3.one * 0.9f);
    }

    public void SetMapRefs(
        Grid newGrid,
        Tilemap newPlayground,
        Tilemap newIndestructibles,
        Tilemap newDestructibles
    )
    {
        grid = newGrid;
        playgroundTilemap = newPlayground;
        indestructibleTilemap = newIndestructibles;
        destructibleTilemap = newDestructibles;

        SnapToNearestValidCell();
    }

    public bool IsMapReady => grid != null;

    public void SetSpeed(float value)
    {
        this.moveSpeed = value; 
    }
}