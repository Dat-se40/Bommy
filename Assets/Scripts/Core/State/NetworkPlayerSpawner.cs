using PurrNet;
using UnityEngine;

public class NetworkPlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject fallbackPlayerPrefab;
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private Grid grid;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject players;

    readonly SyncList<PlayerController> playerControllers = new();

    protected override void OnSpawned()
    {
        base.OnSpawned();

        if (characterDatabase != null)
            MatchSessionBroker.SetCharacterCatalog(characterDatabase);

        networkManager.onPlayerJoined += SpawnPlayer;

        if (grid != null)
        {
            MapRefs map = grid.transform.Find("Map")?.GetComponent<MapRefs>();

            if (map != null)
                spawnPoints = map.SpawnPoints;
        }
    }

    protected override void OnDespawned()
    {
        networkManager.onPlayerJoined -= SpawnPlayer;
        base.OnDespawned();
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

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{nameof(NetworkPlayerSpawner)}: spawnPoints trống.");
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
