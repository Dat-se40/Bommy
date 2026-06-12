using UnityEngine;

public class MapLoader : MonoBehaviour
{
    [System.Serializable]
    public class MapEntry
    {
        public string mapName;
        public MapRefs mapPrefab;
        public LevelConfig levelConfig;
    }

    [Header("Map")]
    [SerializeField] private Grid grid;
    [SerializeField] private Transform mapParent;
    [SerializeField] private MapEntry[] maps;

    [Header("Systems")]
    [SerializeField] private ExplosionCreator explosionCreator;
    [SerializeField] private MovementController localPlayerMovement;
    [SerializeField] private Transform localPlayer;

    private MapRefs currentMap;

    public MapRefs CurrentMap => currentMap;

    private void Start()
    {
        LoadSelectedMap();
    }

    private void LoadSelectedMap()
    {
        string selectedMapName = GameSession.MapName;

        MapEntry entry = FindMapEntry(selectedMapName);

        MapRefs prefab = entry?.mapPrefab;

        if (prefab == null && maps.Length > 0)
            entry = maps[0];

        if (entry == null || entry.mapPrefab == null)
        {
            Debug.LogWarning("Không có map prefab.");
            return;
        }

        LevelRuntime.SetLevel(entry.levelConfig);
        currentMap = Instantiate(entry.mapPrefab, mapParent);

        SetupSystems(currentMap);
        SpawnLocalPlayer(currentMap);
    }

    private MapEntry FindMapEntry(string mapName)
    {
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].mapName == mapName)
                return maps[i];
        }

        return null;
    }

    private void SetupSystems(MapRefs map)
    {
        if (explosionCreator != null)
        {
            explosionCreator.SetMapRefs(
                grid,
                map.Indestructibles,
                map.Destructibles
            );
        }

        if (localPlayerMovement != null)
        {
            localPlayerMovement.SetMapRefs(
                grid,
                map.Playground,
                map.Indestructibles,
                map.Destructibles
            );
        }
    }

    private void SpawnLocalPlayer(MapRefs map)
    {
        if (localPlayer == null)
            return;

        if (map.SpawnPoints == null || map.SpawnPoints.Length == 0)
            return;

        localPlayer.position = map.SpawnPoints[0].position;

        if (localPlayerMovement != null)
            localPlayerMovement.SnapToNearestValidCell();
    }
}
