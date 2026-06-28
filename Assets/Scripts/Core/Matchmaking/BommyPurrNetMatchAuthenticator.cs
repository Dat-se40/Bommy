using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet;
using PurrNet.Authentication;
using PurrNet.Modules;
using PurrNet.Transports;
using UnityEngine;

public sealed class BommyPurrNetMatchAuthenticator : AuthenticationBehaviour<string>
{
    const char Separator = '|';

    static string clientPayload;
    static readonly Dictionary<Connection, string> pendingUserByConnection = new();
    static readonly Dictionary<PlayerID, string> userByPlayer = new();
    static readonly Dictionary<string, PlayerID> playerByUser = new(StringComparer.Ordinal);

    PlayersManager playersManager;

    public static void ConfigureClientPayload(string matchId, string allocationId, string userId)
    {
        clientPayload = string.Join(
            Separator.ToString(),
            Escape(matchId),
            Escape(allocationId),
            Escape(userId)
        );
    }

    public static void ClearClientPayload()
    {
        clientPayload = string.Empty;
    }

    public static void ClearServerBindings()
    {
        pendingUserByConnection.Clear();
        userByPlayer.Clear();
        playerByUser.Clear();
    }

    public static BommyPurrNetMatchAuthenticator EnsureInstalled(NetworkManager manager)
    {
        if (manager == null)
            throw new ArgumentNullException(nameof(manager));

        if (!manager.TryGetComponent(out BommyPurrNetMatchAuthenticator authenticator))
            authenticator = manager.gameObject.AddComponent<BommyPurrNetMatchAuthenticator>();

        if (manager.authenticator != authenticator)
            manager.authenticator = authenticator;

        return authenticator;
    }

    public static bool TryGetUserId(PlayerID player, out string userId)
    {
        return userByPlayer.TryGetValue(player, out userId) && !string.IsNullOrWhiteSpace(userId);
    }

    protected override Task<AuthenticationRequest<string>> GetClientPayload()
    {
        string payload = string.IsNullOrWhiteSpace(clientPayload) ? string.Empty : clientPayload;
        return Task.FromResult(new AuthenticationRequest<string>(payload));
    }

    protected override Task<AuthenticationResponse> ValidateClientPayload(Connection conn, string payload)
    {
        if (!TryParsePayload(payload, out string matchId, out string allocationId, out string userId))
        {
            Debug.LogWarning("[BommyPurrNetMatchAuthenticator] Rejected client with invalid auth payload.");
            return Task.FromResult<AuthenticationResponse>(false);
        }

        if (!DedicatedMatchRuntime.HasLaunchConfig)
        {
            Debug.LogWarning("[BommyPurrNetMatchAuthenticator] Rejected " + userId + " because no launch config is active.");
            return Task.FromResult<AuthenticationResponse>(false);
        }

        if (!string.Equals(matchId, DedicatedMatchRuntime.MatchId, StringComparison.Ordinal)
            || !string.Equals(allocationId, DedicatedMatchRuntime.AllocationId, StringComparison.Ordinal))
        {
            Debug.LogWarning(
                "[BommyPurrNetMatchAuthenticator] Rejected " + userId
                + " for stale match allocation. requested="
                + matchId + "/" + allocationId
                + " active=" + DedicatedMatchRuntime.MatchId + "/" + DedicatedMatchRuntime.AllocationId
            );
            return Task.FromResult<AuthenticationResponse>(false);
        }

        if (!DedicatedMatchRuntime.ContainsUser(userId))
        {
            Debug.LogWarning("[BommyPurrNetMatchAuthenticator] Rejected unknown launch user " + userId + ".");
            return Task.FromResult<AuthenticationResponse>(false);
        }

        pendingUserByConnection[conn] = userId;
        Debug.Log("[BommyPurrNetMatchAuthenticator] Accepted user " + userId + " for match " + matchId + ".");
        return Task.FromResult<AuthenticationResponse>(new AuthenticationResponse
        {
            success = true,
            cookie = userId
        });
    }

    public override void Subscribe(INetworkManager manager, BroadcastModule broadcastModule, PlayersManager players)
    {
        base.Subscribe(manager, broadcastModule, players);
        playersManager = players;
        players.onPrePlayerJoined += OnPrePlayerJoined;
    }

    public override void Unsubscribe(INetworkManager manager, BroadcastModule broadcastModule, PlayersManager players)
    {
        if (playersManager != null)
            playersManager.onPrePlayerJoined -= OnPrePlayerJoined;

        playersManager = null;
        base.Unsubscribe(manager, broadcastModule, players);
    }

    protected override void UnAuthenticateClient(Connection conn)
    {
        pendingUserByConnection.Remove(conn);
    }

    void OnPrePlayerJoined(PlayerID player, bool isReconnect, bool asServer)
    {
        if (!asServer || playersManager == null)
            return;

        if (!playersManager.TryGetConnection(player, out Connection conn))
            return;

        if (!pendingUserByConnection.TryGetValue(conn, out string userId))
            return;

        pendingUserByConnection.Remove(conn);
        userByPlayer[player] = userId;
        playerByUser[userId] = player;

        Debug.Log("[BommyPurrNetMatchAuthenticator] Bound PurrNet player " + player.id + " to Nakama user " + userId + ".");
    }

    static bool TryParsePayload(string payload, out string matchId, out string allocationId, out string userId)
    {
        matchId = string.Empty;
        allocationId = string.Empty;
        userId = string.Empty;

        if (string.IsNullOrWhiteSpace(payload))
            return false;

        string[] parts = payload.Split(Separator);
        if (parts.Length != 3)
            return false;

        matchId = Unescape(parts[0]);
        allocationId = Unescape(parts[1]);
        userId = Unescape(parts[2]);

        return !string.IsNullOrWhiteSpace(matchId)
            && !string.IsNullOrWhiteSpace(allocationId)
            && !string.IsNullOrWhiteSpace(userId);
    }

    static string Escape(string value)
    {
        return Uri.EscapeDataString(value ?? string.Empty);
    }

    static string Unescape(string value)
    {
        return Uri.UnescapeDataString(value ?? string.Empty);
    }
}
