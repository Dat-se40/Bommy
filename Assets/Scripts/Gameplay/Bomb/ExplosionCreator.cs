using PurrNet;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Server authority phá gạch. State ô đã phá nằm trong SyncList — client/late join replay từ list.
/// Tilemap chỉ là view local (MapRefs / destructibleTilemap).
/// </summary>
public class ExplosionCreator : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private Explosion explosionPrefab;
    [SerializeField] private Transform explosionParent;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap indestructibleTilemap;
    [SerializeField] private Tilemap destructibleTilemap;

    [Header("Optional")]
    [SerializeField] private ItemDropper itemDropper;

    [Header("Explosion Settings")]
    [SerializeField] private int explosionRange = 2;
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask playerLayer;

    /// <summary>
    /// Danh sách ô đã phá (server append-only). Late joiner nhận full list khi spawn.
    /// Dùng SyncList thay SyncQueue vì không dequeue — chỉ cần "tập ô đã hủy".
    /// </summary>
    readonly SyncList<Vector3Int> destroyedCells = new();

    public static ExplosionCreator Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();

        destroyedCells.onChanged += OnDestroyedCellsChanged;
        ReplayAllDestroyedCells();
    }

    protected override void OnDespawned()
    {
        destroyedCells.onChanged -= OnDestroyedCellsChanged;
        base.OnDespawned();
    }

    public void CreateExplosionAtCell(Vector3Int originCell)
    {
        if (!isServer)
            return;

        if (grid == null)
        {
            Debug.LogWarning("Grid is not assigned.");
            return;
        }

        SpawnExplosionVisual(originCell);
        AffectCell(originCell);
        Spread(Vector3Int.up, originCell);
        Spread(Vector3Int.down, originCell);
        Spread(Vector3Int.left, originCell);
        Spread(Vector3Int.right, originCell);
    }

    void SpawnExplosionVisual(Vector3Int cell)
    {
        if (explosionPrefab == null)
            return;

        Vector3 position = grid.GetCellCenterWorld(cell);
        Explosion explo = Instantiate(explosionPrefab, position, Quaternion.identity, explosionParent);
        networkManager.Spawn(explo.gameObject);
    }

    void Spread(Vector3Int direction, Vector3Int originCell)
    {
        for (int i = 1; i <= explosionRange; i++)
        {
            Vector3Int targetCell = originCell + direction * i;

            if (HasTile(indestructibleTilemap, targetCell))
                break;

            AffectCell(targetCell);

            if (HasTile(destructibleTilemap, targetCell))
            {
                DestroyDestructible(targetCell);
                break;
            }
        }
    }

    void AffectCell(Vector3Int cell) => DamagePlayersAtCell(cell);

    void DamagePlayersAtCell(Vector3Int cell)
    {
        Vector3 center = grid.GetCellCenterWorld(cell);

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            center,
            new Vector2(0.85f, 0.85f),
            0f,
            playerLayer
        );

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].TryGetComponent(out PlayerInfor playerInfor))
                playerInfor.TakeDamage(damage);
        }
    }

    /// <summary>Server: ghi state + tilemap authority (server tilemap cho physics).</summary>
    void DestroyDestructible(Vector3Int cell)
    {
        if (!isServer)
            return;

        if (destructibleTilemap == null || !destructibleTilemap.HasTile(cell))
            return;

        if (ContainsDestroyedCell(cell))
            return;

        destructibleTilemap.SetTile(cell, null);
        destroyedCells.Add(cell);

        if (itemDropper != null)
            itemDropper.TryDropAt(grid.GetCellCenterWorld(cell));
    }

    bool ContainsDestroyedCell(Vector3Int cell)
    {
        for (int i = 0; i < destroyedCells.Count; i++)
        {
            if (destroyedCells[i] == cell)
                return true;
        }

        return false;
    }

    void OnDestroyedCellsChanged(SyncListChange<Vector3Int> change)
    {
        if (change.operation != SyncListOperation.Added)
            return;

        ApplyDestroyCellLocal(change.value);
    }

    /// <summary>Late join / OnSpawned: áp toàn bộ ô đã phá lên tilemap local.</summary>
    void ReplayAllDestroyedCells()
    {
        for (int i = 0; i < destroyedCells.Count; i++)
            ApplyDestroyCellLocal(destroyedCells[i]);
    }

    static void ApplyDestroyCellLocal(Vector3Int cell)
    {
        if (MapRefs.Instance != null)
            MapRefs.Instance.DestroyCell(cell);
    }

    static bool HasTile(Tilemap tilemap, Vector3Int cell)
    {
        return tilemap != null && tilemap.HasTile(cell);
    }

    public void SetMapRefs(
        Grid newGrid,
        Tilemap newIndestructibleTilemap,
        Tilemap newDestructibleTilemap
    )
    {
        grid = newGrid;
        indestructibleTilemap = newIndestructibleTilemap;
        destructibleTilemap = newDestructibleTilemap;
    }
}
