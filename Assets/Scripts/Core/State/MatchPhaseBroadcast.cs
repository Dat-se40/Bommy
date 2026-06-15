using System;
using PurrNet;
using UnityEngine;

/// <summary>
/// Replicate phase + đồng hồ đếm ngược cho mọi client (UI, gate gameplay).
/// Timer tick trong Update() — không phụ thuộc StateMachine.StateUpdate.
/// </summary>
public class MatchPhaseBroadcast : NetworkBehaviour
{
    static MatchPhaseBroadcast instance;

    public static MatchPhaseBroadcast Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType<MatchPhaseBroadcast>();

            return instance;
        }
    }

    readonly SyncVar<MatchPhaseKind> currentPhase = new();
    readonly SyncVar<float> phaseRemainingSeconds = new();
    readonly SyncVar<float> phaseDurationSeconds = new();

    float serverRemainingSeconds;
    bool serverTicking;

    public MatchPhaseKind CurrentPhase => currentPhase.value;
    public float PhaseRemainingSeconds => phaseRemainingSeconds.value;
    public float PhaseDurationSeconds => phaseDurationSeconds.value;

    public event Action PhaseChanged;
    public event Action PhaseCompleted;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"{nameof(MatchPhaseBroadcast)}: duplicate on '{name}'.", this);
            return;
        }

        instance = this;
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();

        currentPhase.onChanged += OnAnyPhaseReplicated;
        phaseRemainingSeconds.onChanged += OnAnyTimerReplicated;
        phaseDurationSeconds.onChanged += OnAnyTimerReplicated;

        PhaseChanged?.Invoke();
    }

    protected override void OnDespawned()
    {
        currentPhase.onChanged -= OnAnyPhaseReplicated;
        phaseRemainingSeconds.onChanged -= OnAnyTimerReplicated;
        phaseDurationSeconds.onChanged -= OnAnyTimerReplicated;

        serverTicking = false;

        if (instance == this)
            instance = null;

        base.OnDespawned();
    }

    protected override void OnDestroy()
    {
        if (instance == this)
            instance = null;

        base.OnDestroy();
    }

    void Update()
    {
        if (!isServer || !serverTicking)
            return;

        serverRemainingSeconds -= Time.deltaTime;
        phaseRemainingSeconds.value = Mathf.Max(0f, serverRemainingSeconds);

        if (serverRemainingSeconds > 0f)
            return;

        serverTicking = false;
        serverRemainingSeconds = 0f;
        phaseRemainingSeconds.value = 0f;
        PhaseCompleted?.Invoke();
    }

    void OnAnyPhaseReplicated(MatchPhaseKind _) => PhaseChanged?.Invoke();
    void OnAnyTimerReplicated(float _) => PhaseChanged?.Invoke();

    public void ServerStartPhase(MatchPhaseKind phase, float durationSeconds)
    {
        if (!isServer)
            return;

        currentPhase.value = phase;
        phaseDurationSeconds.value = Mathf.Max(0.01f, durationSeconds);
        serverRemainingSeconds = durationSeconds;
        phaseRemainingSeconds.value = serverRemainingSeconds;
        serverTicking = true;
    }

    public void ServerStopPhaseTimer()
    {
        if (!isServer)
            return;

        serverTicking = false;
    }
}
