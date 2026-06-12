using UnityEngine;

public class MapLoader : MonoBehaviour
{
    [System.Serializable]
    public class MapEntry
    {
        public string mapName;
        public MapRefs mapPrefab;
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

        MapRefs prefab = FindMapPrefab(selectedMapName);

        if (prefab == null && maps.Length > 0)
            prefab = maps[0].mapPrefab;

        if (prefab == null)
        {
            Debug.LogWarning("Không có map prefab.");
            return;
        }

        currentMap = Instantiate(prefab, mapParent);

        SetupSystems(currentMap);
        SpawnLocalPlayer(currentMap);
    }

    private MapRefs FindMapPrefab(string mapName)
    {
        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].mapName == mapName)
                return maps[i].mapPrefab;
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
