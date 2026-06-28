using System.Collections.Generic;
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
    readonly List<PlayerID> pendingSpawnPlayers = new();
    readonly List<PlayerID> spawnedPlayers = new();

    protected override void OnSpawned()
    {
        base.OnSpawned();

        if (characterDatabase != null)
            MatchSessionBroker.SetCharacterCatalog(characterDatabase);

        networkManager.onPlayerJoined += SpawnPlayer;
        networkManager.onPlayerLeft += OnPlayerLeft;
        MapLoader.MapReady += OnMapReady;
        TryResolveSpawnPoints();

        if (isServer && networkManager.players != null)
            MatchRoundCoordinator.Instance?.NotifyConnectedPlayerCountChanged(networkManager.players.Count);
    }

    protected override void OnDespawned()
    {
        networkManager.onPlayerJoined -= SpawnPlayer;
        networkManager.onPlayerLeft -= OnPlayerLeft;
        MapLoader.MapReady -= OnMapReady;
        base.OnDespawned();
    }

    void OnMapReady(MapRefs map)
    {
        mapRefs = map;
        TryResolveSpawnPoints();
        TrySpawnPendingPlayers();
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

        MatchRoundCoordinator.Instance?.NotifyConnectedPlayerCountChanged(networkManager.players?.Count ?? 0);
        TrySpawnPlayer(player);
    }

    void OnPlayerLeft(PlayerID player, bool asServer)
    {
        if (!asServer)
            return;

        pendingSpawnPlayers.Remove(player);
        spawnedPlayers.Remove(player);
        MatchRoundCoordinator.Instance?.NotifyConnectedPlayerCountChanged(networkManager.players?.Count ?? 0);
    }

    bool TrySpawnPlayer(PlayerID player)
    {
        if (spawnedPlayers.Contains(player))
            return true;

        Debug.Log($"[FLOW:NETWORK] SpawnPlayer start id={player.id}");

        if (!TryResolveSpawnPoints())
        {
            QueuePendingSpawn(player);
            FlowGuard.Info(
                FlowGuard.TagNetwork,
                "Spawn delayed until map spawn points are ready.",
                this
            );
            return false;
        }

        PlayerMatchProfile profile = ResolveSpawnProfile(player);

        if (!FlowGuard.IsValidSpawnProfile(profile, out string invalidReason))
        {
            FlowGuard.Error(FlowGuard.TagNetwork, $"Spawn aborted: {invalidReason}", this);
            return false;
        }

        GameObject prefab = MatchSessionBroker.ResolvePlayerPrefab(profile, fallbackPlayerPrefab);

        if (!FlowGuard.RequireNotNull(prefab, FlowGuard.TagNetwork, "Player prefab", this))
            return false;

        int spawnIndex = Mathf.Clamp(profile.slotIndex, 0, spawnPoints.Length - 1);

        GameObject newPlayer = Instantiate(
            prefab,
            spawnPoints[spawnIndex].position,
            Quaternion.identity
        );

        newPlayer.transform.SetParent(players.transform);

        if (!newPlayer.TryGetComponent(out PlayerController playerController))
            return false;

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
        spawnedPlayers.Add(player);
        pendingSpawnPlayers.Remove(player);
        FlowGuard.Info(
            FlowGuard.TagNetwork,
            $"Spawned PurrNet player {player.id} at spawnIndex={spawnIndex} slot={profile.slotIndex} userId={profile.userId} displayName={profile.displayName}",
            this
        );
        return true;
    }

    void QueuePendingSpawn(PlayerID player)
    {
        if (pendingSpawnPlayers.Contains(player))
            return;

        pendingSpawnPlayers.Add(player);
    }

    void TrySpawnPendingPlayers()
    {
        if (!isServer || pendingSpawnPlayers.Count == 0)
            return;

        PlayerID[] snapshot = pendingSpawnPlayers.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
            TrySpawnPlayer(snapshot[i]);
    }

    [ObserversRpc(runLocally: false)]
    void BroadcastPlayerBoardRegistration(PlayerMatchProfile profile)
    {
        if (PlayerBoardHub.Instance != null)
            PlayerBoardHub.Instance.OnNetworkPlayerRegistered(profile);
    }

    PlayerMatchProfile ResolveSpawnProfile(PlayerID player)
    {
        if (DedicatedMatchRuntime.TryCreateProfileForJoinedPlayer(player, characterDatabase, out PlayerMatchProfile dedicatedProfile))
            return dedicatedProfile;

        if (DedicatedServerBootstrap.IsDedicatedServerRuntime)
            return CreateDedicatedFallbackProfile(player);

        MatchSessionBroker.LoadLocalFromProgression(characterDatabase);

        PlayerMatchProfile profile = MatchSessionBroker.GetLocalPlayer();
        profile.owner = player;
        profile.isLocal = false;

        return profile;
    }

    PlayerMatchProfile CreateDedicatedFallbackProfile(PlayerID player)
    {
        CharacterDefinition definition = null;
        int characterId = 1;
        int catalogIndex = 0;

        if (characterDatabase != null && characterDatabase.Count > 0)
        {
            definition = characterDatabase.GetByIndex(0);
            if (definition != null)
                characterId = definition.CharacterId;
        }

        PlayerMatchProfile profile = definition != null
            ? PlayerMatchProfile.FromDefinition(
                definition,
                catalogIndex,
                playerControllers.Count,
                false,
                "Player",
                string.Empty
            )
            : new PlayerMatchProfile
            {
                slotIndex = playerControllers.Count,
                characterId = characterId,
                catalogIndex = catalogIndex,
                displayName = "Player",
                hp = 3,
                bomb = 1,
                speed = 4,
                isLocal = false,
                userId = string.Empty
            };

        profile.owner = player;
        MatchSessionBroker.RegisterRemotePlayer(profile);
        return profile;
    }
}
