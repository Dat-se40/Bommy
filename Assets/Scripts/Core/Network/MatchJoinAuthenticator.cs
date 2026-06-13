using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet;
using PurrNet.Authentication;
using PurrNet.Modules;
using PurrNet.Transports;
using UnityEngine;

public sealed class MatchJoinAuthenticator : AuthenticationBehaviour<MatchJoinPayload, MatchJoinDenial>
{
    PlayerProfileApiClient apiClient;
    PlayersManager players;
    readonly Dictionary<string, PlayerMatchProfile> profilesByCookie = new();

    public void Configure(PlayerProfileApiClient client)
    {
        apiClient = client;
    }

    public override void Subscribe(INetworkManager manager, BroadcastModule broadcastModule, PlayersManager playersManager)
    {
        base.Subscribe(manager, broadcastModule, playersManager);
        players = playersManager;
        players.onPrePlayerJoined += OnPrePlayerJoined;
    }

    public override void Unsubscribe(INetworkManager manager, BroadcastModule broadcastModule, PlayersManager playersManager)
    {
        playersManager.onPrePlayerJoined -= OnPrePlayerJoined;
        players = null;
        profilesByCookie.Clear();
        base.Unsubscribe(manager, broadcastModule, playersManager);
    }

    protected override Task<AuthenticationRequest<MatchJoinPayload>> GetClientPayload()
    {
        if (!MatchSessionBroker.TryGetMatchAllocation(out MatchServerAllocation allocation))
        {
            return Task.FromResult(new AuthenticationRequest<MatchJoinPayload>(default));
        }

        PlayerMatchProfile profile = MatchSessionBroker.GetLocalPlayer();
        MatchJoinPayload payload = MatchJoinPayload.FromSession(allocation, profile);

        return Task.FromResult(new AuthenticationRequest<MatchJoinPayload>(payload)
        {
            cookie = BuildCookie(payload)
        });
    }

    protected override async Task<AuthenticationResponse<MatchJoinDenial>> ValidateClientPayload(
        Connection conn,
        MatchJoinPayload payload
    )
    {
        if (string.IsNullOrWhiteSpace(payload.matchId))
            return Deny("missing match id");

        if (apiClient == null)
            apiClient = FindAnyObjectByType<PlayerProfileApiClient>();

        MatchJoinValidationResult result = apiClient != null
            ? await apiClient.ValidateMatchJoinAsync(payload)
            : MatchJoinValidationResult.Offline(payload);

        if (!result.success)
            return Deny(string.IsNullOrWhiteSpace(result.error) ? "join rejected" : result.error);

        string cookie = BuildCookie(payload);
        profilesByCookie[cookie] = result.profile;

        return AuthenticationResponse<MatchJoinDenial>.Accept(cookie);
    }

    protected override void UnAuthenticateClient(Connection conn)
    {
    }

    void OnPrePlayerJoined(PlayerID player, bool isReconnect, bool asServer)
    {
        if (!asServer || players == null)
            return;

        if (!players.TryGetCookie(player, out string cookie))
            return;

        if (!profilesByCookie.TryGetValue(cookie, out PlayerMatchProfile profile))
            return;

        MatchSessionBroker.RegisterNetworkPlayerProfile(player.id.value, profile);
        FlowGuard.Info(
            FlowGuard.TagNetwork,
            $"Authenticated player {player.id.value}: {profile.displayName} characterId={profile.characterId}",
            this
        );
    }

    static AuthenticationResponse<MatchJoinDenial> Deny(string reason)
    {
        return AuthenticationResponse<MatchJoinDenial>.Deny(new MatchJoinDenial
        {
            reason = reason
        });
    }

    static string BuildCookie(MatchJoinPayload payload)
    {
        return $"{payload.matchId}:{payload.matchToken}:{payload.characterId}:{payload.displayName}";
    }
}
