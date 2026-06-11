using PurrNet;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ExplosionCreator : NetworkBehaviour
{
    [Header("References")]
    [SerializeField]
    private Grid grid;

    [SerializeField]
    private Explosion explosionPrefab;

    [SerializeField]
    private Transform explosionParent;

    [Header("Tilemaps")]
    [SerializeField]
    private Tilemap indestructibleTilemap;

    [SerializeField]
    private Tilemap destructibleTilemap;

    [Header("Optional")]
    [SerializeField]
    private ItemDropper itemDropper;

    [Header("Explosion Settings")]
    [SerializeField]
    private int explosionRange = 2;

    [SerializeField]
    private int damage = 1;

    [SerializeField]
    private LayerMask playerLayer;

    public void CreateExplosionAtCell(Vector3Int originCell)
    {
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

    private void SpawnExplosionVisual(Vector3Int cell)
    {
        if (explosionPrefab == null)
        {
            Debug.LogWarning("Explosion Prefab is not assigned.");
            return;
        }

        Vector3 position = grid.GetCellCenterWorld(cell);

        Instantiate(explosionPrefab, position, Quaternion.identity, explosionParent);
    }

    private void Spread(Vector3Int direction, Vector3Int originCell)
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

    private void AffectCell(Vector3Int cell)
    {
        DamagePlayersAtCell(cell);
    }

    private void DamagePlayersAtCell(Vector3Int cell)
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
            PlayerInfor playerInfor = hits[i].GetComponent<PlayerInfor>();

            DamagePlayersRpc(playerInfor);
        }
    }

    [ServerRpc(requireOwnership: false)]
    void DamagePlayersRpc(PlayerInfor playerInfor, RPCInfo rpcInfo = default)
    {
        if (playerInfor != null)
            playerInfor.TakeDamage(damage);
            // TODO: Sync damage to clients by sync var
    }
    private void DestroyDestructible(Vector3Int cell)
    {
        if (!isServer)
            return;

        if (destructibleTilemap == null)
            return;

        if (!destructibleTilemap.HasTile(cell))
            return;

        SyncDestroyTileRpc(cell);

        if (itemDropper != null)
            itemDropper.TryDropAt(grid.GetCellCenterWorld(cell));
    }

    [ObserversRpc]
    void SyncDestroyTileRpc(Vector3Int cell) => destructibleTilemap.SetTile(cell, null);

    private bool HasTile(Tilemap tilemap, Vector3Int cell)
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

    public static ExplosionCreator Instance;

    private void Awake()
    {
        Instance = this;
    }
}
