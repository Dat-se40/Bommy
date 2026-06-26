using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Load map prefab local trên mọi peer khi MatchPhaseBroadcast replicate activeMapId.
/// StateNode chỉ chạy server — không gọi LoadSelectedMap trực tiếp từ Prep state.
/// </summary>
public class MapLoader : MonoBehaviour
{
    public static event Action<MapRefs> MapReady;

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

        if (entry?.mapPrefab == null && maps.Length > 0)
            entry = maps[0];

        if (entry == null || entry.mapPrefab == null)
        {
            FlowGuard.Error(
                FlowGuard.TagGameplay,
                $"MapLoader: không tìm thấy map prefab cho id={selectedMapId}.",
                this
            );
            return;
        }

        UnloadCurrentMap();

        LevelRuntime.SetLevel(entry.levelConfig);
        currentMap = Instantiate(entry.mapPrefab, mapParent);
        currentMap.transform.SetParent(mapParent, false);
        currentMap.transform.localPosition = Vector3.zero;
        currentMap.currentMapId = selectedMapId;
        loadedMapId = selectedMapId;
        SoundManager.Instance.SetSceneLibrary(MapRefs.Instance.GetSoundLibrary);
        SoundManager.Instance.PlayBgm(MapRefs.Instance.mainBgmKey); 
        SetupSystems(currentMap);
        MapReady?.Invoke(currentMap);

        FlowGuard.Info(
            FlowGuard.TagGameplay,
            $"Map loaded id={selectedMapId} prefab={entry.mapPrefab.name}",
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
