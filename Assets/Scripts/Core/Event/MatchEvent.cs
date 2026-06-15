using PurrNet;

/// <summary>
/// Sự kiện gameplay — replicate qua MatchGameplayAuthority.MatchEvents SyncList.
/// Client subscribe để announce / kill feed.
/// </summary>
public struct MatchEvent
{
    public MatchEventType type;
    public PlayerID source;
    public PlayerID target;
    public int value;
}
