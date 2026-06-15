using UnityEngine;

/// <summary>
/// Phase bo thu hẹp (30s). Cell update chưa implement — chỉ giữ timer + flag phase.
/// </summary>
public class MatchZoneShrinkState : MatchTimedStateNode
{
    [SerializeField] private float zoneShrinkDurationSeconds = MatchTiming.ZoneShrinkSeconds;

    protected override MatchPhaseKind PhaseKind => MatchPhaseKind.ZoneShrink;
    protected override float DurationSeconds => zoneShrinkDurationSeconds;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (asServer)
            FlowGuard.Info(FlowGuard.TagSetup, "Zone shrink started (cell update TODO).", this);
    }

    protected override void OnDurationElapsed()
    {
        // Sau bo: match tiếp tục cho đến khi còn 1 người (MatchGameplayAuthority).
        // Không có state tiếp theo — timer dừng ở 00:00.
    }
}
