using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nakama;
using PurrNet;
using UnityEngine;

[Serializable]
public class MatchSettlementPlayerResult
{
    public string userId;
    public int placement;
    public int kills;
    public int deaths;
    public bool disconnected;
}

[Serializable]
public class MatchSettlementRequest
{
    public string serverId;
    public string allocationId;
    public string matchId;
    public string serverSecret;
    public MatchSettlementPlayerResult[] results;
}

[Serializable]
public class MatchSettlementReward
{
    public string userId;
    public int placement;
    public int kills;
    public int deaths;
    public int coinsDelta;
    public int trophiesDelta;
    public int experienceDelta;
    public int winsDelta;
    public int matchesPlayedDelta;
}

[Serializable]
public class MatchSettlementResponse
{
    public bool success;
    public string errorMessage;
    public string settlementId;
    public string matchId;
    public string allocationId;
    public string status;
    public MatchSettlementReward[] rewards;
}

public static class DedicatedMatchRuntime
{
    static MatchLaunchConfig launchConfig;
    static IClient serverClient;
    static string nakamaHttpKey;
    static string serverId;
    static string serverSecret;
    static string provider;
    static int nextPlayerIndex;
    static bool settlementStarted;

    public static event Action MatchLifecycleReleased;

    public static bool HasLaunchConfig => launchConfig != null && launchConfig.success;
    public static string MatchId => launchConfig != null ? launchConfig.matchId : string.Empty;
    public static string AllocationId => launchConfig != null ? launchConfig.allocationId : string.Empty;
    public static int IntendedPlayerCount => launchConfig != null ? launchConfig.IntendedPlayerCount : 0;
    public static int RequestedMapId => launchConfig != null ? Mathf.Max(0, launchConfig.mapId) : 0;

    public static void Configure(
        MatchLaunchConfig config,
        IClient client,
        string httpKey,
        string dedicatedServerId,
        string dedicatedServerSecret,
        string serverProvider
    )
    {
        launchConfig = config;
        serverClient = client;
        nakamaHttpKey = httpKey;
        serverId = dedicatedServerId;
        serverSecret = dedicatedServerSecret;
        provider = string.IsNullOrWhiteSpace(serverProvider) ? "LocalDev" : serverProvider;
        nextPlayerIndex = 0;
        settlementStarted = false;

        MatchSessionBroker.ClearRoster();

        if (launchConfig?.players == null)
            return;

        for (int i = 0; i < launchConfig.players.Length; i++)
        {
            MatchLaunchPlayer player = launchConfig.players[i];
            MatchSessionBroker.RegisterRemotePlayer(new PlayerMatchProfile
            {
                slotIndex = i,
                characterId = Mathf.Max(1, player.selectedCharacterId),
                catalogIndex = -1,
                displayName = string.IsNullOrWhiteSpace(player.displayName) ? player.username : player.displayName,
                isLocal = false,
                userId = player.userId
            });
        }
    }

    public static bool TryCreateProfileForJoinedPlayer(
        PlayerID owner,
        CharacterDatabase characterDatabase,
        out PlayerMatchProfile profile
    )
    {
        profile = default;

        if (launchConfig?.players == null || launchConfig.players.Length == 0)
            return false;

        int slotIndex = Mathf.Clamp(nextPlayerIndex, 0, launchConfig.players.Length - 1);
        nextPlayerIndex++;

        MatchLaunchPlayer launchPlayer = launchConfig.players[slotIndex];
        int characterId = Mathf.Max(1, launchPlayer.selectedCharacterId);
        int catalogIndex = characterDatabase != null ? characterDatabase.GetIndexById(characterId) : -1;
        CharacterDefinition definition = characterDatabase != null ? characterDatabase.GetById(characterId) : null;

        if (definition != null)
        {
            profile = PlayerMatchProfile.FromDefinition(
                definition,
                catalogIndex,
                slotIndex,
                false,
                string.IsNullOrWhiteSpace(launchPlayer.displayName) ? launchPlayer.username : launchPlayer.displayName,
                launchPlayer.userId
            );
        }
        else
        {
            profile = new PlayerMatchProfile
            {
                slotIndex = slotIndex,
                characterId = characterId,
                catalogIndex = Mathf.Max(0, catalogIndex),
                displayName = string.IsNullOrWhiteSpace(launchPlayer.displayName) ? launchPlayer.username : launchPlayer.displayName,
                hp = 3,
                bomb = 1,
                speed = 4,
                isLocal = false,
                userId = launchPlayer.userId
            };
        }

        profile.owner = owner;
        MatchSessionBroker.RegisterRemotePlayer(profile);
        return true;
    }

    public static string GetUserIdForSlot(int slotIndex)
    {
        if (launchConfig?.players == null || slotIndex < 0 || slotIndex >= launchConfig.players.Length)
            return string.Empty;

        return launchConfig.players[slotIndex].userId;
    }

    public static async Task<MatchSettlementResponse> SettleAndReleaseAsync(IReadOnlyList<LeaderBoardData> leaderboard)
    {
        if (settlementStarted)
            return null;

        settlementStarted = true;

        if (serverClient == null || launchConfig == null)
            throw new InvalidOperationException("Dedicated match runtime is not configured.");

        MatchSettlementPlayerResult[] results = BuildResults(leaderboard);
        MatchSettlementRequest request = new()
        {
            serverId = serverId,
            allocationId = launchConfig.allocationId,
            matchId = launchConfig.matchId,
            serverSecret = serverSecret,
            results = results
        };

        IApiRpc response = await serverClient.RpcAsync(nakamaHttpKey, "settle_match", JsonUtility.ToJson(request));
        MatchSettlementResponse settlement = JsonUtility.FromJson<MatchSettlementResponse>(response.Payload);

        if (settlement == null || !settlement.success)
            throw new InvalidOperationException(settlement == null || string.IsNullOrWhiteSpace(settlement.errorMessage)
                ? "Match settlement failed."
                : settlement.errorMessage);

        Debug.Log("[DedicatedMatchRuntime] Settlement completed for match " + launchConfig.matchId + ".");

        await serverClient.RpcAsync(nakamaHttpKey, "reset_match_server", JsonUtility.ToJson(new DedicatedServerRpcRequest
        {
            serverId = serverId,
            allocationId = launchConfig.allocationId,
            matchId = launchConfig.matchId,
            serverSecret = serverSecret,
            status = "Available",
            reason = "Match settled"
        }));

        ClearForNextMatch();
        MatchLifecycleReleased?.Invoke();
        return settlement;
    }

    public static async Task CancelAndResetAsync(string reason)
    {
        if (settlementStarted)
            return;

        settlementStarted = true;

        if (serverClient == null || launchConfig == null)
            throw new InvalidOperationException("Dedicated match runtime is not configured.");

        await serverClient.RpcAsync(nakamaHttpKey, "reset_match_server", JsonUtility.ToJson(new DedicatedServerRpcRequest
        {
            serverId = serverId,
            allocationId = launchConfig.allocationId,
            matchId = launchConfig.matchId,
            serverSecret = serverSecret,
            status = "Available",
            reason = string.IsNullOrWhiteSpace(reason) ? "Match cancelled" : reason
        }));

        ClearForNextMatch();
        MatchLifecycleReleased?.Invoke();
    }

    public static void ClearForNextMatch()
    {
        launchConfig = null;
        serverClient = null;
        nakamaHttpKey = string.Empty;
        serverId = string.Empty;
        serverSecret = string.Empty;
        provider = string.Empty;
        nextPlayerIndex = 0;
        settlementStarted = false;
        MatchSessionBroker.ClearRoster();
    }

    public static int ResolveMapId(MapLoader mapLoader, int fallbackMapId)
    {
        if (RequestedMapId > 0)
        {
            Debug.Log("[DedicatedMatchRuntime] Using requested map id " + RequestedMapId + ".");
            return RequestedMapId;
        }

        if (mapLoader != null && mapLoader.TryResolveRandomMapId(out int randomMapId))
        {
            Debug.Log("[DedicatedMatchRuntime] Random map resolved to id " + randomMapId + ".");
            return randomMapId;
        }

        Debug.LogWarning("[DedicatedMatchRuntime] Random map unavailable; using fallback map id " + fallbackMapId + ".");
        return fallbackMapId;
    }

    static MatchSettlementPlayerResult[] BuildResults(IReadOnlyList<LeaderBoardData> leaderboard)
    {
        List<MatchSettlementPlayerResult> results = new();

        if (leaderboard != null)
        {
            for (int i = 0; i < leaderboard.Count; i++)
            {
                LeaderBoardData entry = leaderboard[i];
                string userId = string.IsNullOrWhiteSpace(entry.userId)
                    ? GetUserIdForSlot(entry.slotIndex)
                    : entry.userId;

                if (string.IsNullOrWhiteSpace(userId))
                    continue;

                results.Add(new MatchSettlementPlayerResult
                {
                    userId = userId,
                    placement = i + 1,
                    kills = Mathf.Max(0, entry.kills),
                    deaths = Mathf.Max(0, entry.deaths),
                    disconnected = entry.disconnected
                });
            }
        }

        if (results.Count == 0 && launchConfig?.players != null)
        {
            for (int i = 0; i < launchConfig.players.Length; i++)
            {
                results.Add(new MatchSettlementPlayerResult
                {
                    userId = launchConfig.players[i].userId,
                    placement = i + 1,
                    kills = 0,
                    deaths = 0,
                    disconnected = true
                });
            }
        }

        return results.ToArray();
    }
}
