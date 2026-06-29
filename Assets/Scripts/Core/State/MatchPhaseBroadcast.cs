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
    readonly SyncVar<float> matchElapsedSeconds = new();
    readonly SyncVar<int> activeMapId = new(NoMapId);
    readonly SyncVar<int> connectedPlayerCount = new();
    readonly SyncVar<int> intendedPlayerCount = new();
    readonly SyncVar<bool> matchCancelled = new();
    readonly SyncVar<string> cancellationReason = new();

    float serverRemainingSeconds;
    float serverMatchElapsedSeconds;
    bool serverTicking;

    public MatchPhaseKind CurrentPhase => currentPhase.value;
    public float PhaseRemainingSeconds => phaseRemainingSeconds.value;
    public float PhaseDurationSeconds => phaseDurationSeconds.value;
    public float MatchElapsedSeconds => matchElapsedSeconds.value;
    public int ActiveMapId => activeMapId.value;
    public int ConnectedPlayerCount => connectedPlayerCount.value;
    public int IntendedPlayerCount => intendedPlayerCount.value;
    public bool MatchCancelled => matchCancelled.value;
    public string CancellationReason => cancellationReason.value;

    public event Action PhaseChanged;
    public event Action PhaseCompleted;
    public event Action<int> MapIdChanged;
    public event Action WaitingForPlayersChanged;

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
        matchElapsedSeconds.onChanged += OnAnyTimerReplicated;
        activeMapId.onChanged += OnMapIdReplicated;
        connectedPlayerCount.onChanged += OnAnyWaitingReplicated;
        intendedPlayerCount.onChanged += OnAnyWaitingReplicated;
        matchCancelled.onChanged += OnAnyWaitingReplicated;
        cancellationReason.onChanged += OnAnyWaitingReplicated;

        PhaseChanged?.Invoke();
        WaitingForPlayersChanged?.Invoke();
        NotifyMapIdIfValid(activeMapId.value);
    }

    protected override void OnDespawned()
    {
        currentPhase.onChanged -= OnAnyPhaseReplicated;
        phaseRemainingSeconds.onChanged -= OnAnyTimerReplicated;
        phaseDurationSeconds.onChanged -= OnAnyTimerReplicated;
        matchElapsedSeconds.onChanged -= OnAnyTimerReplicated;
        activeMapId.onChanged -= OnMapIdReplicated;
        connectedPlayerCount.onChanged -= OnAnyWaitingReplicated;
        intendedPlayerCount.onChanged -= OnAnyWaitingReplicated;
        matchCancelled.onChanged -= OnAnyWaitingReplicated;
        cancellationReason.onChanged -= OnAnyWaitingReplicated;

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

        float deltaTime = Time.deltaTime;
        serverRemainingSeconds -= deltaTime;

        if (currentPhase.value == MatchPhaseKind.Gameplay || currentPhase.value == MatchPhaseKind.ZoneShrink)
        {
            serverMatchElapsedSeconds += deltaTime;
            matchElapsedSeconds.value = Mathf.Max(0f, serverMatchElapsedSeconds);
        }

        phaseRemainingSeconds.value = Mathf.Max(0f, serverRemainingSeconds);

        if (serverRemainingSeconds > 0f)
            return;

        serverTicking = false;
        serverRemainingSeconds = 0f;
        phaseRemainingSeconds.value = 0f;
        matchElapsedSeconds.value = Mathf.Max(0f, serverMatchElapsedSeconds);
        PhaseCompleted?.Invoke();
    }

    void OnAnyPhaseReplicated(MatchPhaseKind _) => PhaseChanged?.Invoke();
    void OnAnyTimerReplicated(float _) => PhaseChanged?.Invoke();
    void OnMapIdReplicated(int mapId) => NotifyMapIdIfValid(mapId);
    void OnAnyWaitingReplicated<T>(T _)
    {
        WaitingForPlayersChanged?.Invoke();
        PhaseChanged?.Invoke();
    }

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

        matchCancelled.value = false;
        cancellationReason.value = string.Empty;

        if (phase == MatchPhaseKind.Gameplay)
        {
            serverMatchElapsedSeconds = 0f;
            matchElapsedSeconds.value = 0f;
        }

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

    public void ServerStartWaitingForPlayers(int connectedCount, int intendedCount)
    {
        if (!isServer)
            return;

        serverTicking = false;
        serverMatchElapsedSeconds = 0f;
        currentPhase.value = MatchPhaseKind.WaitingForPlayers;
        phaseDurationSeconds.value = 0f;
        phaseRemainingSeconds.value = 0f;
        matchElapsedSeconds.value = 0f;
        matchCancelled.value = false;
        cancellationReason.value = string.Empty;
        ServerSetWaitingForPlayers(connectedCount, intendedCount);
    }

    public void ServerSetWaitingForPlayers(int connectedCount, int intendedCount)
    {
        if (!isServer)
            return;

        connectedPlayerCount.value = Mathf.Max(0, connectedCount);
        intendedPlayerCount.value = Mathf.Max(0, intendedCount);
    }

    public void ServerCancelMatch(string reason)
    {
        if (!isServer)
            return;

        serverTicking = false;
        phaseRemainingSeconds.value = 0f;
        matchElapsedSeconds.value = Mathf.Max(0f, serverMatchElapsedSeconds);
        matchCancelled.value = true;
        cancellationReason.value = string.IsNullOrWhiteSpace(reason) ? "Match cancelled" : reason;
    }
}
