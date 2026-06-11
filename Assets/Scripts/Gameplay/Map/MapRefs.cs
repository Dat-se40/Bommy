using UnityEngine;
using UnityEngine.Tilemaps;

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
    public static MapRefs Instance;

    private void Awake()
    {
        Instance = this;
    }
}
