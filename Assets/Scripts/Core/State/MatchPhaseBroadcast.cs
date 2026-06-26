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

    public const int NoMapId = -1;

    readonly SyncVar<MatchPhaseKind> currentPhase = new();
    readonly SyncVar<float> phaseRemainingSeconds = new();
    readonly SyncVar<float> phaseDurationSeconds = new();
    readonly SyncVar<int> activeMapId = new(NoMapId);

    float serverRemainingSeconds;
    bool serverTicking;

    public MatchPhaseKind CurrentPhase => currentPhase.value;
    public float PhaseRemainingSeconds => phaseRemainingSeconds.value;
    public float PhaseDurationSeconds => phaseDurationSeconds.value;
    public int ActiveMapId => activeMapId.value;

    public event Action PhaseChanged;
    public event Action PhaseCompleted;
    public event Action<int> MapIdChanged;

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
        activeMapId.onChanged += OnMapIdReplicated;

        PhaseChanged?.Invoke();
        NotifyMapIdIfValid(activeMapId.value);
    }

    protected override void OnDespawned()
    {
        currentPhase.onChanged -= OnAnyPhaseReplicated;
        phaseRemainingSeconds.onChanged -= OnAnyTimerReplicated;
        phaseDurationSeconds.onChanged -= OnAnyTimerReplicated;
        activeMapId.onChanged -= OnMapIdReplicated;

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
    void OnMapIdReplicated(int mapId) => NotifyMapIdIfValid(mapId);

    void NotifyMapIdIfValid(int mapId)
    {
        if (mapId < 0)
            return;

        MapIdChanged?.Invoke(mapId);
    }

    /// <summary>Server — replicate map id cho mọi client (load visual + spawn points).</summary>
    public void ServerSetActiveMap(int mapId)
    {
        if (!isServer)
            return;

        activeMapId.value = mapId;
    }

    public void ServerStartPhase(MatchPhaseKind phase, float durationSeconds)
    {
        switch (phase)
        {
            case MatchPhaseKind.None:
                break;
            case MatchPhaseKind.Prep:
                break;
            case MatchPhaseKind.Gameplay:
                SoundManager.Instance.StopAllSfx();
                SoundManager.Instance.PlaySfx(SoundKey.SfxEnterGameState);
                break;
            case MatchPhaseKind.ZoneShrink:
                SoundManager.Instance.StopAllSfx();
                SoundManager.Instance.PlaySfx(SoundKey.SfxEnterShrinkState);
                break;
            default:
                break;
        }
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
