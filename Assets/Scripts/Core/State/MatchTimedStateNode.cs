using PurrNet.StateMachine;
using UnityEngine;

/// <summary>
/// StateNode phase có thời lượng — timer chạy trên MatchPhaseBroadcast.Update (server).
/// </summary>
public abstract class MatchTimedStateNode : StateNode
{
    [SerializeField] private MatchPhaseBroadcast phaseBroadcast;

    protected abstract MatchPhaseKind PhaseKind { get; }
    protected abstract float DurationSeconds { get; }

    protected MatchPhaseBroadcast PhaseBroadcast => phaseBroadcast;

    public override void Enter(bool asServer)
    {
        if (!asServer)
            return;

        MatchPhaseBroadcast broadcast = ResolveBroadcast();
        if (broadcast == null)
            return;

        broadcast.PhaseCompleted += HandlePhaseCompleted;
        broadcast.ServerStartPhase(PhaseKind, DurationSeconds);
    }

    public override void Exit(bool asServer)
    {
        if (!asServer)
            return;

        MatchPhaseBroadcast broadcast = ResolveBroadcast();
        if (broadcast == null)
            return;

        broadcast.PhaseCompleted -= HandlePhaseCompleted;
        broadcast.ServerStopPhaseTimer();
    }

    void HandlePhaseCompleted()
    {
        if (!isCurrentState)
            return;

        OnDurationElapsed();
    }

    protected virtual void OnDurationElapsed()
    {
        machine.Next();
    }

    MatchPhaseBroadcast ResolveBroadcast()
    {
        if (phaseBroadcast != null)
            return phaseBroadcast;

        return MatchPhaseBroadcast.Instance;
    }
}
