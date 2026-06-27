using UnityEngine;

/// <summary>
/// Server starts Prep, resolves the concrete map, and replicates it to every client.
/// </summary>
public class MatchPrepState : MatchTimedStateNode
{
    [SerializeField] private float prepDurationSeconds = MatchTiming.PrepSeconds;
    [SerializeField] private int fallbackMapId = 2;
    [SerializeField] private bool inTestingMode;

    protected override MatchPhaseKind PhaseKind => MatchPhaseKind.Prep;
    protected override float DurationSeconds => prepDurationSeconds;

    public override void Enter(bool asServer)
    {
        if (IsDedicatedWaitingForLaunchConfig(asServer))
        {
            FlowGuard.Info(FlowGuard.TagSetup, "Dedicated server is idle; Prep is waiting for Nakama launch config.", this);
            return;
        }

        base.Enter(asServer);

        if (!asServer)
            return;

        int resolvedMapId = ResolveMapId();
        MatchPhaseBroadcast broadcast = PhaseBroadcast;

        if (broadcast == null)
        {
            FlowGuard.Error(
                FlowGuard.TagGameplay,
                "MatchPrepState: missing MatchPhaseBroadcast; cannot replicate map id.",
                machine
            );
            return;
        }

        GameSession.MapId = resolvedMapId;
        broadcast.ServerSetActiveMap(resolvedMapId);
        FlowGuard.Info(
            FlowGuard.TagGameplay,
            $"Prep started - activeMapId={resolvedMapId} requestedMapId={DedicatedMatchRuntime.RequestedMapId}"
        );
    }

    int ResolveMapId()
    {
        if (DedicatedServerBootstrap.IsDedicatedServerRuntime && DedicatedMatchRuntime.HasLaunchConfig)
            return DedicatedMatchRuntime.ResolveMapId(MapLoader.Instance, fallbackMapId);

        if (GameSession.MapId >= 0 && !inTestingMode)
            return GameSession.MapId;

        return fallbackMapId;
    }
}
