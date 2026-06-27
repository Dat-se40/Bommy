using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;

/// <summary>
/// PurrNet StateMachine bỏ qua SetState(state[0]) khi host (isPlannedHost).
/// Bootstrap này đảm bảo match luôn vào Prep state trên server.
/// </summary>
public class MatchStateMachineBootstrap : NetworkBehaviour
{
    [SerializeField] private StateMachine stateMachine;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        MatchRoundCoordinator coordinator = MatchRoundCoordinator.EnsureOn(gameObject);
        coordinator.Configure(this, isServer);

        if (!isServer)
            return;

        if (DedicatedServerBootstrap.IsDedicatedServerRuntime)
            return;

        StartFirstStateIfNeeded();
    }

    public void StartFirstStateIfNeeded(bool forceEnterCurrentState = false)
    {
        if (stateMachine == null)
            stateMachine = GetComponent<StateMachine>();

        if (stateMachine == null)
        {
            FlowGuard.Error(
                FlowGuard.TagSetup,
                $"{nameof(MatchStateMachineBootstrap)}: missing StateMachine reference.",
                this
            );
            return;
        }

        var states = stateMachine.states;
        if (states == null || states.Count == 0)
        {
            FlowGuard.Error(
                FlowGuard.TagSetup,
                $"{nameof(MatchStateMachineBootstrap)}: StateMachine has no states.",
                this
            );
            return;
        }

        if (stateMachine.currentState.stateId >= 0)
        {
            if (forceEnterCurrentState)
            {
                states[0].Enter(true);
                FlowGuard.Info(FlowGuard.TagSetup, "Match state machine entered first state after dedicated allocation.", this);
            }

            return;
        }

        stateMachine.SetState(states[0]);
        FlowGuard.Info(FlowGuard.TagSetup, "Match state machine started at first state (host bootstrap).", this);
    }
}
