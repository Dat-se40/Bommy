namespace Backend.Models;

public sealed record CreateMatchRequest(
    string? RoomName,
    string? MapName,
    int MaxPlayers,
    int CharacterId,
    string? DisplayName
);

public sealed record CreateMatchResponse(
    bool Success,
    MatchServerAllocation? Allocation,
    string? Error
);

public sealed record MatchServerAllocation(
    string MatchId,
    string Host,
    int Port,
    string MatchToken,
    string EdgegapDeploymentId
);

public sealed record ServerReadyRequest(string? EdgegapDeploymentId, int Port);
public sealed record ServerReadyResponse(bool Success, MatchServerAllocation Allocation);

public sealed record MatchJoinPayload(
    string? MatchId,
    string? MatchToken,
    string? PlayerAccessToken,
    int CharacterId,
    int CatalogIndex,
    string? DisplayName,
    int Hp,
    int Bomb,
    int Speed
);

public sealed record MatchJoinValidationResult(
    bool Success,
    PlayerMatchProfile? Profile,
    string? Error
)
{
    public static MatchJoinValidationResult Accepted(PlayerMatchProfile profile)
    {
        return new MatchJoinValidationResult(true, profile, null);
    }

    public static MatchJoinValidationResult Denied(string error)
    {
        return new MatchJoinValidationResult(false, null, error);
    }
}
