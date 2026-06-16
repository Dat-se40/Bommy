/// <summary>
/// Thời lượng các phase match (giây). Gameplay + ZoneShrink = 3 phút chơi.
/// </summary>
public static class MatchTiming
{
    public const float PrepSeconds = 5f;
    public const float GameplaySeconds = 5f;
    public const float ZoneShrinkSeconds = 120f;
    public const float TotalGameplaySeconds = GameplaySeconds + ZoneShrinkSeconds;
}
