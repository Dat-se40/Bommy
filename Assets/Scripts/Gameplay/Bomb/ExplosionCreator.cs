using PurrNet;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Sensor bomb/explosion — phá gạch, detect hit, Submit vào MatchGameplayAuthority.
/// Không giữ match events / game-over state (thuộc MatchGameplayAuthority).
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

    readonly SyncList<Vector3Int> destroyedCells = new();
    readonly SyncList<Vector3Int> shrinkCells = new();

    public static ExplosionCreator Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();

        destroyedCells.onChanged += OnDestroyedCellsChanged;
        shrinkCells.onChanged += OnShrinkCellsChanged;
        ReplayAllDestroyedCells();
        ReplayAllShrinkCells();
    }

    protected override void OnDespawned()
    {
        destroyedCells.onChanged -= OnDestroyedCellsChanged;
        shrinkCells.onChanged -= OnShrinkCellsChanged;
        base.OnDespawned();
    }

    /// <summary>Server — thêm ô bo vào SyncList (client + join muộn replay giống destroyedCells).</summary>
    public void ServerAddShrinkCell(Vector3Int cell)
    {
        if (!isServer)
            return;
        shrinkCells.Add(cell);
    }
    public bool IsShrinkCell(Vector3Int cell)
    {
        return ContainsShrinkCell(cell);
    }
    /// <summary>Gọi khi MapRefs spawn sau network spawn.</summary>
    public void ReplayShrinkCellsOnMapReady()
    {
        ReplayAllShrinkCells();
    }

    bool ContainsShrinkCell(Vector3Int cell)
    {
        for (int i = 0; i < shrinkCells.Count; i++)
        {
            if (shrinkCells[i] == cell)
                return true;
        }

        return false;
    }

    void OnShrinkCellsChanged(SyncListChange<Vector3Int> change)
    {
        if (change.operation != SyncListOperation.Added)
            return;

        ApplyShrinkCellLocal(change.value);
    }

    void ReplayAllShrinkCells()
    {
        for (int i = 0; i < shrinkCells.Count; i++)
            ApplyShrinkCellLocal(shrinkCells[i]);
    }

    static void ApplyShrinkCellLocal(Vector3Int cell)
    {
        if (MapRefs.Instance == null)
            return;

        MapRefs.Instance.ApplyShrinkCell(cell);
    }

    public void CreateExplosionAtCell(Vector3Int originCell, PlayerID? creator)
    {
        if (!isServer)
            return;

        if (grid == null)
        {
            Debug.LogWarning("Grid is not assigned.");
            return;
        }

        PlayerID attacker = creator ?? PlayerID.Server;

        SpawnExplosionVisual(originCell);
        AffectCell(originCell, attacker);
        Spread(Vector3Int.up, originCell, attacker);
        Spread(Vector3Int.down, originCell, attacker);
        Spread(Vector3Int.left, originCell, attacker);
        Spread(Vector3Int.right, originCell, attacker);
    }

    void SpawnExplosionVisual(Vector3Int cell)
    {
        if (explosionPrefab == null)
            return;

        Vector3 position = grid.GetCellCenterWorld(cell);
        Explosion explo = Instantiate(explosionPrefab, position, Quaternion.identity, explosionParent);
        networkManager.Spawn(explo.gameObject);
    }

    void Spread(Vector3Int direction, Vector3Int originCell, PlayerID attacker)
    {
        for (int i = 1; i <= explosionRange; i++)
        {
            Vector3Int targetCell = originCell + direction * i;

            if (HasTile(indestructibleTilemap, targetCell))
                break;

            AffectCell(targetCell, attacker);

            if (HasTile(destructibleTilemap, targetCell))
            {
                DestroyDestructible(targetCell, attacker);
                break;
            }
        }
    }

    void AffectCell(Vector3Int cell, PlayerID attacker) => DamagePlayersAtCell(cell, attacker);

    void DamagePlayersAtCell(Vector3Int cell, PlayerID attacker)
    {
        if (!isServer)
            return;

        MatchGameplayAuthority authority = MatchGameplayAuthority.Instance;
        if (authority == null)
        {
            FlowGuard.Error(
                FlowGuard.TagNetwork,
                "MatchGameplayAuthority missing — thêm GameObject MatchSystems vào GameScene.",
                this
            );
            return;
        }

        Vector3 center = grid.GetCellCenterWorld(cell);

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            center,
            new Vector2(0.85f, 0.85f),
            0f,
            playerLayer
        );

        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].TryGetComponent(out PlayerInfor playerInfor))
                continue;

            PlayerID? injured = playerInfor.GetPlayerID();
            if (!injured.HasValue)
                continue;

            authority.SubmitAttack(
                new AttackDTO
                {
                    attacker = attacker,
                    injured = injured.Value,
                    damage = this.damage,
                }
            );
        }
    }

    void DestroyDestructible(Vector3Int cell, PlayerID scorer)
    {
        if (!isServer)
            return;

        if (destructibleTilemap == null || !destructibleTilemap.HasTile(cell))
            return;

        if (ContainsDestroyedCell(cell))
            return;

        destructibleTilemap.SetTile(cell, null);
        destroyedCells.Add(cell);

        MatchGameplayAuthority authority = MatchGameplayAuthority.Instance;
        if (authority != null && scorer != PlayerID.Server)
            authority.GrantScore(scorer, General.SCORE_DESTROY_OBSTACLE);

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
        SoundPlayback.PlaySynced(SoundKey.SfxExplosion);
    }

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
