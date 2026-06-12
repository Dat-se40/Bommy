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

    void SpawnPlayer(PlayerID player, bool isReconnect, bool asServer)
    {
        if (!asServer)
            return;

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

        if (newPlayer.TryGetComponent(out PlayerSpawnSetup spawnSetup))
            spawnSetup.Apply(profile);

        if (newPlayer.TryGetComponent(out PlayerController playerController))
        {
            playerController.GiveOwnership(player);
            networkManager.Spawn(newPlayer);
            playerControllers.Add(playerController);

            // TODO[NETWORK] Broadcast profile lên PlayerBoardHub qua ObserversRpc.
            RegisterPlayerOnHubRpc(profile);
        }
    }

    PlayerMatchProfile ResolveSpawnProfile(PlayerID player)
    {
        // TODO[NETWORK] Map PlayerID → profile đã gửi lúc join lobby.
        MatchSessionBroker.LoadLocalFromPlayerPrefs(characterDatabase);

        PlayerMatchProfile profile = MatchSessionBroker.GetLocalPlayer();
        profile.isLocal = true;

        return profile;
    }

    [ObserversRpc]
    void RegisterPlayerOnHubRpc(PlayerMatchProfile profile, RPCInfo rpcInfo = default)
    {
        PlayerBoardHub hub = FindFirstObjectByType<PlayerBoardHub>();

        if (hub != null)
            hub.OnNetworkPlayerRegistered(profile);
    }
}
