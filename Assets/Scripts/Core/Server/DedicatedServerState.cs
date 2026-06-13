public static class DedicatedServerState
{
    public static bool IsReady { get; private set; }
    public static string MatchId { get; private set; }
    public static string EdgegapDeploymentId { get; private set; }

    public static void MarkReady(string matchId, string edgegapDeploymentId)
    {
        IsReady = true;
        MatchId = matchId;
        EdgegapDeploymentId = edgegapDeploymentId;

        FlowGuard.Info(
            FlowGuard.TagNetwork,
            $"Dedicated server ready matchId={matchId ?? "-"} edgegap={edgegapDeploymentId ?? "-"}"
        );
    }
}
