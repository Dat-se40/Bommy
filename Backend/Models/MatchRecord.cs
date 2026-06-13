namespace Backend.Models;

public sealed record MatchRecord(
    string MatchId,
    string Host,
    int Port,
    string MatchToken,
    string EdgegapDeploymentId,
    string? MapName,
    int MaxPlayers,
    bool Ready
)
{
    public static MatchRecord Create(
        string matchId,
        string host,
        int port,
        string edgegapDeploymentId,
        string? mapName,
        int maxPlayers
    )
    {
        return new MatchRecord(
            matchId,
            host,
            port,
            "match_" + Guid.NewGuid().ToString("N"),
            edgegapDeploymentId,
            mapName,
            maxPlayers,
            Ready: false
        );
    }

    public MatchServerAllocation ToAllocation()
    {
        return new MatchServerAllocation(
            MatchId,
            Host,
            Port,
            MatchToken,
            EdgegapDeploymentId
        );
    }
}
