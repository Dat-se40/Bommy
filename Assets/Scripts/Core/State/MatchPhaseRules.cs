/// <summary>
/// Gate client/server cho bomb, skill theo phase hiện tại.
/// </summary>
public static class MatchPhaseRules
{
    public static bool CanPlaceBomb =>
        MatchPhaseBroadcast.Instance != null &&
        MatchPhaseBroadcast.Instance.CurrentPhase != MatchPhaseKind.Prep &&
        MatchPhaseBroadcast.Instance.CurrentPhase != MatchPhaseKind.None;

    public static bool CanUseSkill => CanPlaceBomb;

    public static bool IsZoneShrinking =>
        MatchPhaseBroadcast.Instance != null &&
        MatchPhaseBroadcast.Instance.CurrentPhase == MatchPhaseKind.ZoneShrink;
}
