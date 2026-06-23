using UnityEngine;

/// <summary>
/// Phase chuẩn bị — tự mở bảng map info, player có thể E / đóng trong lúc Prep.
/// </summary>
public class MatchPrepState : MatchTimedStateNode
{
    [SerializeField] private float prepDurationSeconds = MatchTiming.PrepSeconds;

    protected override MatchPhaseKind PhaseKind => MatchPhaseKind.Prep;
    protected override float DurationSeconds => prepDurationSeconds;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            MapInfoDialogController.Instance?.BeginPrepPhase();
    }

    public override void Exit(bool asServer)
    {
        if (!asServer)
            MapInfoDialogController.Instance?.EndPrepPhase();

        base.Exit(asServer);
    }
}
