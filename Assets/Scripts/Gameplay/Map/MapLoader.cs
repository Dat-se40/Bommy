using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Load map prefab local trên mọi peer khi MatchPhaseBroadcast replicate activeMapId.
/// StateNode chỉ chạy server — không gọi LoadSelectedMap trực tiếp từ Prep state.
/// </summary>
public class MapLoader : MonoBehaviour
{
    static MapLoader instance;

    public static event Action<MapRefs> MapReady;
    public static MapLoader Instance => instance;

    [Serializable]
    public class MapEntry
    {
        public int mapId;
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
    MapRefs currentMap;
    int loadedMapId = MatchPhaseBroadcast.NoMapId;
    Coroutine bindBroadcastRoutine;

    public MapRefs CurrentMap => currentMap;
    public int LoadedMapId => loadedMapId;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"{nameof(MapLoader)}: duplicate on '{name}'.", this);
            return;
        }

        instance = this;
        LevelRuntime.Clear();
        ValidateMapEntries();
    }

    void OnEnable()
    {
        bindBroadcastRoutine = StartCoroutine(BindBroadcastWhenReady());
    }

    void OnDisable()
    {
        if (bindBroadcastRoutine != null)
        {
            StopCoroutine(bindBroadcastRoutine);
            bindBroadcastRoutine = null;
        }

        UnsubscribeBroadcast();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    IEnumerator BindBroadcastWhenReady()
    {
        while (MatchPhaseBroadcast.Instance == null)
            yield return null;

        SubscribeBroadcast(MatchPhaseBroadcast.Instance);
    }

    void SubscribeBroadcast(MatchPhaseBroadcast broadcast)
    {
        if (broadcast == null)
            return;

        broadcast.MapIdChanged -= OnActiveMapIdChanged;
        broadcast.MapIdChanged += OnActiveMapIdChanged;

        if (broadcast.ActiveMapId >= 0)
            OnActiveMapIdChanged(broadcast.ActiveMapId);
    }

    void UnsubscribeBroadcast()
    {
        MatchPhaseBroadcast broadcast = MatchPhaseBroadcast.Instance;

        if (broadcast == null)
            return;

        broadcast.MapIdChanged -= OnActiveMapIdChanged;
    }

    void OnActiveMapIdChanged(int mapId)
    {
        LoadSelectedMap(mapId, forceReload: false);
    }

    /// <summary>
    /// Load hoặc giữ map đang có. forceReload dùng khi reconnect cần refresh MapReady.
    /// </summary>
    public void LoadSelectedMap(int selectedMapId, bool forceReload = false)
    {
        if (selectedMapId < 0)
            return;

        if (!forceReload
            && currentMap != null
            && loadedMapId == selectedMapId)
        {
            MapReady?.Invoke(currentMap);
            return;
        }

        MapEntry entry = FindMapEntry(selectedMapId);

        if (entry == null)
        {
            FlowGuard.Error(
                FlowGuard.TagGameplay,
                $"MapLoader: no map entry configured for id={selectedMapId}.",
                this
            );
            return;
        }

        if (entry.mapPrefab == null)
        {
            FlowGuard.Error(
                FlowGuard.TagGameplay,
                $"MapLoader: map id={selectedMapId} has no prefab configured.",
                this
            );
            return;
        }

        if (entry.levelConfig == null)
        {
            FlowGuard.Error(
                FlowGuard.TagGameplay,
                $"MapLoader: map id={selectedMapId} has no LevelConfig configured.",
                this
            );
            return;
        }

        UnloadCurrentMap();

        LevelRuntime.SetLevel(entry.levelConfig);
        currentMap = Instantiate(entry.mapPrefab, mapParent);
        currentMap.transform.SetParent(mapParent, false);
        currentMap.transform.localPosition = Vector3.zero;
        currentMap.currentMapId = entry.mapId;
        loadedMapId = entry.mapId;
        SoundManager.Instance.SetSceneLibrary(MapRefs.Instance.GetSoundLibrary);
        SoundManager.Instance.PlayBgm(MapRefs.Instance.mainBgmKey);
        SetupSystems(currentMap);
        MapReady?.Invoke(currentMap);

        FlowGuard.Info(
            FlowGuard.TagGameplay,
            $"Map loaded id={entry.mapId} config={entry.levelConfig.levelId} prefab={entry.mapPrefab.name}",
            this
        );
    }

    /// <summary>Reconnect — map id đã biết nhưng cần re-bind spawn / tilemaps.</summary>
    public void ReloadForReconnect()
    {
        int mapId = loadedMapId;

        if (mapId < 0)
        {
            MatchPhaseBroadcast broadcast = MatchPhaseBroadcast.Instance;

            if (broadcast != null && broadcast.ActiveMapId >= 0)
                mapId = broadcast.ActiveMapId;
        }

        if (mapId < 0)
            return;

        LoadSelectedMap(mapId, forceReload: true);
    }

    void UnloadCurrentMap()
    {
        if (currentMap == null)
            return;

        Destroy(currentMap.gameObject);
        currentMap = null;
        loadedMapId = MatchPhaseBroadcast.NoMapId;
        LevelRuntime.Clear();
    }

    MapEntry FindMapEntry(int id)
    {
        if (maps == null)
            return null;

        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].mapId == id)
                return maps[i];
        }

        return null;
    }

    void ValidateMapEntries()
    {
        if (maps == null || maps.Length == 0)
        {
            Debug.LogWarning("MapLoader has no maps configured.", this);
            return;
        }

        HashSet<int> seenIds = new HashSet<int>();

        for (int i = 0; i < maps.Length; i++)
        {
            MapEntry entry = maps[i];

            if (entry == null)
            {
                Debug.LogWarning($"MapLoader map entry {i} is null.", this);
                continue;
            }

            if (entry.mapId <= 0)
                Debug.LogWarning($"MapLoader map entry {i} has invalid mapId={entry.mapId}.", this);

            if (!seenIds.Add(entry.mapId))
                Debug.LogWarning($"MapLoader has duplicate mapId={entry.mapId}.", this);

            if (entry.mapPrefab == null)
                Debug.LogWarning($"MapLoader mapId={entry.mapId} has no prefab.", this);

            if (entry.levelConfig == null)
            {
                Debug.LogWarning($"MapLoader mapId={entry.mapId} has no LevelConfig.", this);
                continue;
            }

            if (TryParseMapId(entry.levelConfig.levelId, out int configMapId)
                && configMapId != entry.mapId)
            {
                Debug.LogWarning(
                    $"MapLoader mapId={entry.mapId} does not match LevelConfig.levelId={entry.levelConfig.levelId}.",
                    this
                );
            }
        }
    }

    static bool TryParseMapId(string levelId, out int mapId)
    {
        mapId = MatchPhaseBroadcast.NoMapId;

        if (string.IsNullOrWhiteSpace(levelId))
            return false;

        const string Prefix = "map_";
        string trimmed = levelId.Trim();

        if (!trimmed.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        return int.TryParse(trimmed.Substring(Prefix.Length), out mapId) && mapId > 0;
    }

    public bool TryResolveRandomMapId(out int mapId)
    {
        mapId = MatchPhaseBroadcast.NoMapId;

        int[] mapIds = GetAvailableMapIds();
        if (mapIds.Length == 0)
            return false;

        mapId = mapIds[UnityEngine.Random.Range(0, mapIds.Length)];
        return true;
    }

    public int[] GetAvailableMapIds()
    {
        if (maps == null || maps.Length == 0)
            return Array.Empty<int>();

        int count = 0;
        for (int i = 0; i < maps.Length; i++)
        {
            if (IsUsableMapEntry(maps[i]))
                count++;
        }

        if (count <= 0)
            return Array.Empty<int>();

        int[] ids = new int[count];
        int index = 0;

        for (int i = 0; i < maps.Length; i++)
        {
            if (IsUsableMapEntry(maps[i]))
                ids[index++] = maps[i].mapId;
        }

        return ids;
    }

    static bool IsUsableMapEntry(MapEntry entry)
    {
        return entry != null
            && entry.mapId > 0
            && entry.mapPrefab != null
            && entry.levelConfig != null;
    }

    void SetupSystems(MapRefs map)
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
}
