using PurrNet;
using UnityEngine;

public class NetworkPlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject fallbackPlayerPrefab;
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private Grid grid;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject players;
    [SerializeField] private MapRefs mapRefs;
    readonly SyncList<PlayerController> playerControllers = new();

    protected override void OnSpawned()
    {
        base.OnSpawned();

        if (characterDatabase != null)
            MatchSessionBroker.SetCharacterCatalog(characterDatabase);

        networkManager.onPlayerJoined += SpawnPlayer;
        MapLoader.MapReady += OnMapReady;
        TryResolveSpawnPoints();
    }

    protected override void OnDespawned()
    {
        networkManager.onPlayerJoined -= SpawnPlayer;
        MapLoader.MapReady -= OnMapReady;
        base.OnDespawned();
    }

    void OnMapReady(MapRefs map)
    {
        mapRefs = map;
        TryResolveSpawnPoints();
    }

    /// <summary>
    /// SpawnPoints nằm trong map prefab — MapLoader instantiate sau scene load.
    /// Gọi lại mỗi lần spawn vì onPlayerJoined có thể tới trước MapLoader.Start.
    /// </summary>
    bool TryResolveSpawnPoints()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return true;

        if (mapRefs == null)
            mapRefs = MapRefs.Instance;

        if (mapRefs == null || mapRefs.SpawnPoints == null || mapRefs.SpawnPoints.Length == 0)
            return false;

        spawnPoints = mapRefs.SpawnPoints;
        FlowGuard.Info(
            FlowGuard.TagNetwork,
            $"Resolved {spawnPoints.Length} spawn point(s) from {mapRefs.name}",
            this
        );
        return true;
    }

    /// <summary>
    /// Callback server từ PurrNet — KHÔNG phải ServerRpc.
    /// ServerRpc chỉ chạy khi client gọi qua mạng; event này đã chạy trên server rồi.
    /// </summary>
    void SpawnPlayer(PlayerID player, bool isReconnect, bool asServer)
    {
        if (!asServer)
            return;

        Debug.Log($"[FLOW:NETWORK] SpawnPlayer start id={player.id}");

        PlayerMatchProfile profile = ResolveSpawnProfile(player);

        if (!FlowGuard.IsValidSpawnProfile(profile, out string invalidReason))
        {
            FlowGuard.Error(FlowGuard.TagNetwork, $"Spawn aborted: {invalidReason}", this);
            return;
        }

        GameObject prefab = MatchSessionBroker.ResolvePlayerPrefab(profile, fallbackPlayerPrefab);

        if (!FlowGuard.RequireNotNull(prefab, FlowGuard.TagNetwork, "Player prefab", this))
            return;

        int playerCount = playerControllers.Count;

        if (!TryResolveSpawnPoints())
        {
            FlowGuard.Error(
                FlowGuard.TagNetwork,
                "spawnPoints trống — MapLoader chưa load map hoặc map prefab thiếu SpawnPoints.",
                this
            );
            return;
        }

        int spawnIndex = Mathf.Clamp(playerCount, 0, spawnPoints.Length - 1);
        profile.slotIndex = spawnIndex;

        GameObject newPlayer = Instantiate(
            prefab,
            spawnPoints[spawnIndex].position,
            Quaternion.identity
        );

        newPlayer.transform.SetParent(players.transform);

        if (!newPlayer.TryGetComponent(out PlayerController playerController))
            return;

        playerController.GiveOwnership(player);
        networkManager.Spawn(newPlayer);
        playerControllers.Add(playerController);

        // Apply + init board SAU Spawn — NetworkBehaviour phải spawned mới ghi SyncVar/RPC được.
        if (newPlayer.TryGetComponent(out PlayerSpawnSetup spawnSetup))
            spawnSetup.Apply(profile);

        // Host: refresh ngay. Client: ObserversRpc + PlayerBoardState SyncVar → RegisterBoardState.
        if (PlayerBoardHub.Instance != null)
            PlayerBoardHub.Instance.OnNetworkPlayerRegistered(profile);

        BroadcastPlayerBoardRegistration(profile);
    }

    [ObserversRpc(runLocally: false)]
    void BroadcastPlayerBoardRegistration(PlayerMatchProfile profile)
    {
        if (PlayerBoardHub.Instance != null)
            PlayerBoardHub.Instance.OnNetworkPlayerRegistered(profile);
    }

    PlayerMatchProfile ResolveSpawnProfile(PlayerID player)
    {
        // TODO[NETWORK] Map PlayerID → profile đã gửi lúc join lobby.
        MatchSessionBroker.LoadLocalFromProgression(characterDatabase);

        PlayerMatchProfile profile = MatchSessionBroker.GetLocalPlayer();
        profile.owner = player;
        profile.isLocal = false;

        return profile;
    }

}
