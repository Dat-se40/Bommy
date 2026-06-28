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
    [SerializeField] private LayerMask playerLayer;

    const float CellOverlapSize = 0.75f;
    static readonly Vector2 CellOverlapExtents = new Vector2(CellOverlapSize, CellOverlapSize);

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    private Vector3Int currentCell;
    private Vector3Int targetCell;
    private Vector2 targetWorldPosition;
    private bool isMoving;
    private int direction;

    PlayerBoardState boardState;

    readonly SyncVar<float> networkMoveSpeed = new();
    readonly SyncVar<Vector3Int> networkCell = new();
    readonly SyncVar<bool> networkCellInitialized = new();
    readonly SyncVar<int> networkDirection = new();

    public Vector3Int CurrentCell => currentCell;
    public bool IsMoving => isMoving;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        boardState = GetComponent<PlayerBoardState>();
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();

        networkMoveSpeed.onChanged += OnNetworkMoveSpeedChanged;
        networkCell.onChanged += OnNetworkCellChanged;
        networkCellInitialized.onChanged += OnNetworkCellInitializedChanged;
        networkDirection.onChanged += OnNetworkDirectionChanged;

        if (networkMoveSpeed.value > 0f)
            moveSpeed = networkMoveSpeed.value;

        if (networkCellInitialized.value)
            ApplyNetworkCell(networkCell.value, snap: true);
    }

    protected override void OnDespawned()
    {
        networkMoveSpeed.onChanged -= OnNetworkMoveSpeedChanged;
        networkCell.onChanged -= OnNetworkCellChanged;
        networkCellInitialized.onChanged -= OnNetworkCellInitializedChanged;
        networkDirection.onChanged -= OnNetworkDirectionChanged;
        base.OnDespawned();
    }

    void OnNetworkMoveSpeedChanged(float value)
    {
        if (value > 0f)
            moveSpeed = value;
    }

    void OnNetworkCellChanged(Vector3Int cell)
    {
        ApplyNetworkCell(cell, snap: !networkCellInitialized.value);
    }

    void OnNetworkCellInitializedChanged(bool initialized)
    {
        if (initialized)
            ApplyNetworkCell(networkCell.value, snap: true);
    }

    void OnNetworkDirectionChanged(int value)
    {
        if (!isOwner)
            direction = value;
    }

    private void Update()
    {
        if (!isOwner) return;
        if (IsPlayerDead()) return; 
        if (grid == null) return;
        if (!isMoving)
            ReadInput();

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (!isOwner)
        {
            MoveRemoteToReplicatedCell();
            return;
        }

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

        if (isServer)
            PublishServerCell(cell);

        if (isOwner && !isServer)
            ReportCurrentCellRpc(cell, direction);
    }

    [ServerRpc]
    void ReportCurrentCellRpc(Vector3Int cell, int reportedDirection, RPCInfo rpcInfo = default)
    {
        currentCell = cell;
        targetCell = cell;
        direction = reportedDirection;
        SnapTransformToCell(cell);
        PublishServerCell(cell);
    }

    void PublishServerCell(Vector3Int cell)
    {
        networkCell.value = cell;
        networkDirection.value = direction;
        networkCellInitialized.value = true;
    }

    void ApplyNetworkCell(Vector3Int cell, bool snap)
    {
        if (isOwner || !networkCellInitialized.value || grid == null)
            return;

        targetCell = cell;
        targetWorldPosition = grid.GetCellCenterWorld(cell);

        if (snap)
        {
            currentCell = cell;
            SnapTransformToCell(cell);
            isMoving = false;
            return;
        }

        isMoving = true;
    }

    void MoveRemoteToReplicatedCell()
    {
        if (!isMoving || rb == null || grid == null)
            return;

        Vector2 newPosition = Vector2.MoveTowards(
            rb.position,
            targetWorldPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(newPosition);

        if (Vector2.Distance(newPosition, targetWorldPosition) > 0.001f)
            return;

        rb.MovePosition(targetWorldPosition);
        currentCell = targetCell;
        isMoving = false;
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

        SnapTransformToCell(startCell);
    }

    void SnapTransformToCell(Vector3Int cell)
    {
        if (grid == null)
            return;

        Vector3 center = grid.GetCellCenterWorld(cell);
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

    /// <summary>
    /// Di chuyển: tile hợp lệ, không có bomb, không có player khác (bỏ qua chính mình).
    /// </summary>
    public bool CanStandAtCell(Vector3Int cell)
    {
        if (!IsWalkableTile(cell))
            return false;

        if (HasBombAtCell(cell))
            return false;

        if (HasOtherPlayerAtCell(cell))
            return false;

        return true;
    }

    /// <summary>
    /// Đặt bomb: tile hợp lệ và chưa có bomb. Player đang đứng trên ô vẫn được phép.
    /// </summary>
    public bool CanPlaceBombAtCell(Vector3Int cell)
    {
        if (!IsWalkableTile(cell))
            return false;

        if (HasBombAtCell(cell))
            return false;

        return true;
    }

    bool IsWalkableTile(Vector3Int cell)
    {
        if (grid == null)
            return false;

        if (playgroundTilemap != null && !playgroundTilemap.HasTile(cell))
            return false;

        if (indestructibleTilemap != null && indestructibleTilemap.HasTile(cell))
            return false;

        if (destructibleTilemap != null && destructibleTilemap.HasTile(cell))
            return false;

        return true;
    }

    bool HasBombAtCell(Vector3Int cell)
    {
        if (grid == null)
            return false;

        Vector3 cellCenter = grid.GetCellCenterWorld(cell);
        return Physics2D.OverlapBox(cellCenter, CellOverlapExtents, 0f, bombLayer) != null;
    }

    bool HasOtherPlayerAtCell(Vector3Int cell)
    {
        if (grid == null)
            return false;

        Vector3 cellCenter = grid.GetCellCenterWorld(cell);
        Collider2D[] hits = Physics2D.OverlapBoxAll(cellCenter, CellOverlapExtents, 0f, playerLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null || IsOwnCollider(hit))
                continue;

            return true;
        }

        return false;
    }

    bool IsOwnCollider(Collider2D collider)
    {
        if (collider == null)
            return false;

        if (collider.attachedRigidbody != null && rb != null && collider.attachedRigidbody == rb)
            return true;

        return collider.transform == transform || collider.transform.IsChildOf(transform);
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
        if (value <= 0f)
            return;

        moveSpeed = value;

        if (isServer)
            networkMoveSpeed.value = value;
    }

    bool IsPlayerDead()
    {
        if (boardState != null)
            return boardState.CurrentHp <= 0 || boardState.IsEliminated;

        if (TryGetComponent(out PlayerInfor playerInfor))
            return playerInfor.IsDead;

        return false;
    }
}
