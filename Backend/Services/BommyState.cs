using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Backend.Models;
using Backend.Options;
using Microsoft.Extensions.Options;

namespace Backend.Services;

public sealed class BommyState(
    IOptions<BommyBackendOptions> options,
    ILogger<BommyState> logger
)
{
    readonly ConcurrentDictionary<string, PlayerAccountSnapshot> accountsByToken = new();
    readonly ConcurrentDictionary<string, MatchRecord> matchesById = new();
    readonly BommyBackendOptions options = options.Value;

    public AuthResponse AuthenticateDev(string devId, string displayName)
    {
        string userId = "dev:" + devId;
        string accessToken = IssueToken("dev", devId);
        PlayerAccountSnapshot account = CreateAccount(userId, null, displayName);

        accountsByToken[accessToken] = account;
        logger.LogInformation("Dev auth accepted devId={DevId} userId={UserId}", devId, userId);

        return new AuthResponse(accessToken, account);
    }

    public AuthResponse AuthenticateSteamTicket(string steamTicket)
    {
        string ticketHash = ShortHash(steamTicket);
        string userId = "steam:" + ticketHash;
        string accessToken = IssueToken("steam", ticketHash);
        PlayerAccountSnapshot account = CreateAccount(userId, ticketHash, "Steam " + ticketHash[..6]);

        accountsByToken[accessToken] = account;
        logger.LogInformation("Steam auth stub accepted ticketHash={TicketHash} userId={UserId}", ticketHash, userId);

        return new AuthResponse(accessToken, account);
    }

    public bool TryGetAccount(string accessToken, out PlayerAccountSnapshot? account)
    {
        return accountsByToken.TryGetValue(accessToken, out account);
    }

    public bool TryPurchaseCharacter(
        string accessToken,
        int characterId,
        out PlayerAccountSnapshot? account
    )
    {
        account = null;

        if (characterId <= 0 ||
            !accountsByToken.TryGetValue(accessToken, out PlayerAccountSnapshot? current))
        {
            return false;
        }

        if (!current.OwnedCharacterIds.Contains(characterId))
        {
            current = current with
            {
                OwnedCharacterIds = current.OwnedCharacterIds.Append(characterId).Order().ToArray()
            };

            accountsByToken[accessToken] = current;
        }

        account = current;
        return true;
    }

    public CreateMatchResponse CreateMatch(CreateMatchRequest request)
    {
        string matchId = string.IsNullOrWhiteSpace(request.RoomName)
            ? "local-match-" + Guid.NewGuid().ToString("N")[..8]
            : request.RoomName.Trim();

        MatchRecord record = matchesById.AddOrUpdate(
            matchId,
            _ => MatchRecord.Create(
                matchId,
                options.LocalHost,
                options.DefaultGameServerPort,
                "local",
                request.MapName,
                request.MaxPlayers
            ),
            (_, existing) => existing with
            {
                MapName = string.IsNullOrWhiteSpace(request.MapName) ? existing.MapName : request.MapName,
                MaxPlayers = request.MaxPlayers > 0 ? request.MaxPlayers : existing.MaxPlayers
            }
        );

        if (!record.Ready)
        {
            logger.LogWarning(
                "Match allocation requested but server is not ready matchId={MatchId} host={Host} port={Port}",
                record.MatchId,
                record.Host,
                record.Port
            );

            return new CreateMatchResponse(
                false,
                null,
                $"match server is not ready for {record.MatchId}"
            );
        }

        logger.LogInformation(
            "Match allocation returned matchId={MatchId} host={Host} port={Port}",
            record.MatchId,
            record.Host,
            record.Port
        );

        return new CreateMatchResponse(true, record.ToAllocation(), null);
    }

    public MatchServerAllocation MarkServerReady(string matchId, ServerReadyRequest request)
    {
        MatchRecord record = matchesById.AddOrUpdate(
            matchId,
            _ => MatchRecord.Create(
                matchId,
                options.LocalHost,
                request.Port > 0 ? request.Port : options.DefaultGameServerPort,
                string.IsNullOrWhiteSpace(request.EdgegapDeploymentId) ? "local" : request.EdgegapDeploymentId,
                null,
                4
            ) with
            {
                Ready = true
            },
            (_, existing) => existing with
            {
                Port = request.Port > 0 ? request.Port : existing.Port,
                EdgegapDeploymentId = string.IsNullOrWhiteSpace(request.EdgegapDeploymentId)
                    ? existing.EdgegapDeploymentId
                    : request.EdgegapDeploymentId,
                Ready = true
            }
        );

        logger.LogInformation(
            "Server ready matchId={MatchId} host={Host} port={Port} edgegap={EdgegapDeploymentId}",
            record.MatchId,
            record.Host,
            record.Port,
            record.EdgegapDeploymentId
        );

        return record.ToAllocation();
    }

    public MatchJoinValidationResult ValidatePlayerJoin(string routeMatchId, MatchJoinPayload payload)
    {
        if (string.IsNullOrWhiteSpace(routeMatchId) ||
            string.IsNullOrWhiteSpace(payload.MatchId) ||
            !string.Equals(routeMatchId, payload.MatchId, StringComparison.Ordinal))
        {
            return MatchJoinValidationResult.Denied("match id mismatch");
        }

        if (!matchesById.TryGetValue(routeMatchId, out MatchRecord? match))
            return MatchJoinValidationResult.Denied("unknown match");

        if (!string.Equals(match.MatchToken, payload.MatchToken, StringComparison.Ordinal))
            return MatchJoinValidationResult.Denied("invalid match token");

        if (string.IsNullOrWhiteSpace(payload.PlayerAccessToken) ||
            !accountsByToken.ContainsKey(payload.PlayerAccessToken))
        {
            return MatchJoinValidationResult.Denied("missing player credentials");
        }

        if (payload.CharacterId <= 0 || string.IsNullOrWhiteSpace(payload.DisplayName))
            return MatchJoinValidationResult.Denied("invalid player profile");

        PlayerMatchProfile profile = new(
            SlotIndex: 0,
            CharacterId: payload.CharacterId,
            CatalogIndex: payload.CatalogIndex,
            DisplayName: payload.DisplayName,
            Hp: payload.Hp,
            Bomb: payload.Bomb,
            Speed: payload.Speed,
            IsLocal: false
        );

        logger.LogInformation(
            "Player join accepted matchId={MatchId} displayName={DisplayName} characterId={CharacterId}",
            routeMatchId,
            profile.DisplayName,
            profile.CharacterId
        );

        return MatchJoinValidationResult.Accepted(profile);
    }

    static PlayerAccountSnapshot CreateAccount(string userId, string? steamId, string displayName)
    {
        return new PlayerAccountSnapshot(
            UserId: userId,
            SteamId: steamId,
            DisplayName: displayName,
            Gold: 850,
            Level: 1,
            OwnedCharacterIds: [1]
        );
    }

    static string IssueToken(string provider, string subject)
    {
        return provider + "_" + ShortHash(subject + ":" + Guid.NewGuid().ToString("N"));
    }

    static string ShortHash(string value)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..16];
    }
}
