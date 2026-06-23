using UnityEngine;

/// <summary>
/// Phase chuẩn bị — server replicate map id + timer; client load map / UI qua SyncVar listeners.
/// </summary>
public class MatchPrepState : MatchTimedStateNode
{
    [SerializeField] private float prepDurationSeconds = MatchTiming.PrepSeconds;
    [SerializeField] private int fallbackMapId = 2;
    // Ignore data from server
    [SerializeField] private bool inTestingMode; 
    protected override MatchPhaseKind PhaseKind => MatchPhaseKind.Prep;
    protected override float DurationSeconds => prepDurationSeconds;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            return;

        int resolvedMapId = (GameSession.MapId >= 0 ) && !inTestingMode ? GameSession.MapId : fallbackMapId;
        MatchPhaseBroadcast broadcast = PhaseBroadcast;

        if (broadcast == null)
        {
            FlowGuard.Error(
                FlowGuard.TagGameplay,
                "MatchPrepState: thiếu MatchPhaseBroadcast — không replicate map id.",
                machine
            );
            return;
        }

        broadcast.ServerSetActiveMap(resolvedMapId);
        FlowGuard.Info(
            FlowGuard.TagGameplay,
            $"Prep started — activeMapId={resolvedMapId} (GameSession.MapId={GameSession.MapId})"
        );
    }
}
