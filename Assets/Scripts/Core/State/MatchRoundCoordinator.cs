using System;
using System.Threading.Tasks;
using PurrNet;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MatchRoundCoordinator : MonoBehaviour
{
    const string DefaultCancellationReason = "Match cancelled";

    static MatchRoundCoordinator instance;

    MatchStateMachineBootstrap bootstrap;
    MatchPhaseBroadcast broadcast;
    bool configuredAsServer;
    bool stateMachineStarted;
    bool cancellationStarted;
    bool gameplayStarted;
    int connectedPlayerCount;

    public static MatchRoundCoordinator Instance => instance;

    public static MatchRoundCoordinator EnsureOn(GameObject host)
    {
        if (instance != null)
            return instance;

        MatchRoundCoordinator coordinator = host.GetComponent<MatchRoundCoordinator>();
        if (coordinator == null)
            coordinator = host.AddComponent<MatchRoundCoordinator>();

        instance = coordinator;
        return coordinator;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }

    void OnDestroy()
    {
        UnsubscribeBroadcast();

        if (instance == this)
            instance = null;
    }

    public void Configure(MatchStateMachineBootstrap owner, bool asServer)
    {
        bootstrap = owner;
        configuredAsServer = asServer;
        ResolveBroadcast();

        if (configuredAsServer && DedicatedServerBootstrap.IsDedicatedServerRuntime)
            PublishWaitingState();
    }

    void Update()
    {
        if (!configuredAsServer || !DedicatedServerBootstrap.IsDedicatedServerRuntime)
            return;

        if (stateMachineStarted || cancellationStarted)
            return;

        PublishWaitingState();
        TryStartWhenReady();
    }

    public void NotifyConnectedPlayerCountChanged(int connectedCount)
    {
        if (!configuredAsServer || !DedicatedServerBootstrap.IsDedicatedServerRuntime)
            return;

        connectedPlayerCount = Mathf.Max(0, connectedCount);
        PublishWaitingState(connectedCount);

        if (cancellationStarted || gameplayStarted)
            return;

        if (!stateMachineStarted)
        {
            TryStartWhenReady();
            return;
        }

        if (IsBeforeGameplay() && connectedPlayerCount < RequiredPlayerCount())
            _ = CancelAndResetAsync(DefaultCancellationReason);
    }

    void TryStartWhenReady()
    {
        if (bootstrap == null || !DedicatedMatchRuntime.HasLaunchConfig)
            return;

        int required = RequiredPlayerCount();
        if (required <= 0)
            return;

        int connected = ConnectedPlayerCount();
        if (connected != required)
            return;

        stateMachineStarted = true;
        Debug.Log("[MatchRoundCoordinator] Intended player count reached. Starting Prep countdown. players=" + connected + "/" + required, this);
        bootstrap.StartFirstStateIfNeeded(forceEnterCurrentState: true);
    }

    bool IsBeforeGameplay()
    {
        MatchPhaseKind phase = broadcast != null ? broadcast.CurrentPhase : MatchPhaseKind.None;
        return phase == MatchPhaseKind.WaitingForPlayers
            || phase == MatchPhaseKind.Prep
            || phase == MatchPhaseKind.None;
    }

    int RequiredPlayerCount()
    {
        if (!DedicatedMatchRuntime.HasLaunchConfig)
            return 0;

        int intended = DedicatedMatchRuntime.IntendedPlayerCount;
        return intended > 0 ? intended : 1;
    }

    int ConnectedPlayerCount()
    {
        if (connectedPlayerCount > 0)
            return connectedPlayerCount;

        NetworkManager manager = NetworkManager.main;
        return manager != null && manager.players != null ? manager.players.Count : 0;
    }

    void PublishWaitingState()
    {
        PublishWaitingState(ConnectedPlayerCount());
    }

    void PublishWaitingState(int connectedCount)
    {
        ResolveBroadcast();

        if (broadcast == null || stateMachineStarted)
            return;

        broadcast.ServerStartWaitingForPlayers(connectedCount, RequiredPlayerCount());
    }

    async Task CancelAndResetAsync(string reason)
    {
        if (cancellationStarted)
            return;

        cancellationStarted = true;
        ResolveBroadcast();
        broadcast?.ServerCancelMatch(reason);
        Debug.LogWarning("[MatchRoundCoordinator] " + reason + ". Resetting dedicated match without settlement.", this);

        await Task.Delay(TimeSpan.FromSeconds(1));

        try
        {
            await DedicatedMatchRuntime.CancelAndResetAsync(reason);
        }
        catch (Exception exception)
        {
            Debug.LogError("[MatchRoundCoordinator] Match cancellation reset failed: " + exception.Message, this);
        }
    }

    void ResolveBroadcast()
    {
        MatchPhaseBroadcast next = MatchPhaseBroadcast.Instance;
        if (next == broadcast)
            return;

        UnsubscribeBroadcast();
        broadcast = next;

        if (broadcast != null)
        {
            broadcast.PhaseChanged += OnPhaseChanged;
            broadcast.WaitingForPlayersChanged += OnWaitingForPlayersChanged;
        }
    }

    void UnsubscribeBroadcast()
    {
        if (broadcast == null)
            return;

        broadcast.PhaseChanged -= OnPhaseChanged;
        broadcast.WaitingForPlayersChanged -= OnWaitingForPlayersChanged;
    }

    void OnPhaseChanged()
    {
        if (broadcast == null)
            return;

        MatchPhaseKind phase = broadcast.CurrentPhase;
        gameplayStarted |= phase == MatchPhaseKind.Gameplay || phase == MatchPhaseKind.ZoneShrink;
    }

    async void OnWaitingForPlayersChanged()
    {
        if (configuredAsServer || broadcast == null || !broadcast.MatchCancelled)
            return;

        Debug.LogWarning("[MatchRoundCoordinator] " + broadcast.CancellationReason + ". Returning to main menu.", this);
        await MatchConnectionService.EnsureExists().DisconnectAsync();
        SceneManager.LoadScene("MainMenu");
    }
}
