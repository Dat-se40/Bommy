using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tham chiếu tilemap trong map prefab. Chỉ xử lý view local — không giữ state network.
/// State ô đã phá: ExplosionCreator.destroyedCells (SyncList trên server).
/// </summary>
public class MapRefs : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap background;
    [SerializeField] private Tilemap playground;
    [SerializeField] private Tilemap indestructibles;
    [SerializeField] private Tilemap destructibles;
    [SerializeField] private Tilemap decorate;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    public Tilemap Background => background;
    public Tilemap Playground => playground;
    public Tilemap Indestructibles => indestructibles;
    public Tilemap Destructibles => destructibles;
    public Tilemap Decorate => decorate;
    public Transform[] SpawnPoints => spawnPoints;

    public static MapRefs Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Xóa tile trên destructible layer — gọi từ mọi client khi SyncList báo ô đã phá.</summary>
    public void DestroyCell(Vector3Int cell)
    {
        if (destructibles != null)
            destructibles.SetTile(cell, null);
    }
}
