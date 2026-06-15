/// <summary>
/// Thời lượng các phase match (giây). Gameplay + ZoneShrink = 3 phút chơi.
/// </summary>
public static class MatchTiming
{
    public const float PrepSeconds = 30f;
    public const float GameplaySeconds = 150f;
    public const float ZoneShrinkSeconds = 30f;
    public const float TotalGameplaySeconds = GameplaySeconds + ZoneShrinkSeconds;
}
