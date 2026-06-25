using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tham chiếu tilemap trong map prefab — chỉ view local.
/// Ô đã phá: ExplosionCreator.destroyedCells.
/// Ô bo: ExplosionCreator.shrinkCells.
/// </summary>
public class MapRefs : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField]
    private Tilemap background;

    [SerializeField]
    private Tilemap playground;

    [SerializeField]
    private Tilemap indestructibles;

    [SerializeField]
    private Tilemap destructibles;

    [SerializeField]
    private Tilemap decorate;

    [SerializeField]
    private Tilemap shrinkZone;

    [Header("Spawn Points")]
    [SerializeField]
    private Transform[] spawnPoints;

    [Header("Shrink Zone")]
    [SerializeField]
    private Tile blockedTile;

    public Tilemap Background => background;
    public Tilemap Playground => playground;
    public Tilemap Indestructibles => indestructibles;
    public Tilemap Destructibles => destructibles;
    public Tilemap Decorate => decorate;
    public Tilemap Shrink => shrinkZone;
    public Transform[] SpawnPoints => spawnPoints;
    public int currentMapId; 
    public static MapRefs Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (ExplosionCreator.Instance != null)
            ExplosionCreator.Instance.ReplayShrinkCellsOnMapReady();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Xóa tile destructible — gọi khi SyncList explosion báo ô đã phá.</summary>
    public void DestroyCell(Vector3Int cell)
    {
        if (destructibles != null)
            destructibles.SetTile(cell, null);
    }

    /// <summary>Apply bo một ô — dùng cho sync client + replay join muộn.</summary>
    public void ApplyShrinkCell(Vector3Int cell)
    {
        if(!shrinkZone.HasTile(cell))
            return;

        if (blockedTile == null)
        {
            FlowGuard.Error(FlowGuard.TagGameplay, "MapRefs.blockedTile chưa gán — bo không hiển thị.", this);
            return;
        }

        if (shrinkZone != null )
            shrinkZone.SetTile(cell, blockedTile);

        if (playground != null)
            playground.SetTile(cell, null);

        if (indestructibles != null)
            indestructibles.SetTile(cell, null);

        if (destructibles != null)
            destructibles.SetTile(cell, null);
    }
}
