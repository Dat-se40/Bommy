using UnityEngine;

/// <summary>
/// Phase chuẩn bị: chờ 30s, không đặt bom / dùng skill.
/// </summary>
public class MatchPrepState : MatchTimedStateNode
{
    [SerializeField] private float prepDurationSeconds = MatchTiming.PrepSeconds;

    protected override MatchPhaseKind PhaseKind => MatchPhaseKind.Prep;
    protected override float DurationSeconds => prepDurationSeconds;
}
