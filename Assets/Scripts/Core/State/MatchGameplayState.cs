using UnityEngine;

/// <summary>
/// Phase chơi chính (2.5 phút). Bom/skill được phép.
/// Sau phase này chuyển sang ZoneShrink (30s cuối của 3 phút match).
/// </summary>
public class MatchGameplayState : MatchTimedStateNode
{
    [SerializeField] private float gameplayDurationSeconds = MatchTiming.GameplaySeconds;

    protected override MatchPhaseKind PhaseKind => MatchPhaseKind.Gameplay;
    protected override float DurationSeconds => gameplayDurationSeconds;
}
