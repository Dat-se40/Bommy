using System;
using System.Threading;
using System.Threading.Tasks;
using PurrNet;
using PurrNet.Transports;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MatchConnectionService : MonoBehaviour
{
    const string SingletonName = "[MatchConnectionService]";

    static MatchConnectionService instance;

    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private float connectTimeoutSeconds = 12f;

    public static MatchConnectionService Instance => instance;
    public bool IsConnected => NetworkManager.main != null && NetworkManager.main.clientState == ConnectionState.Connected;

    public static MatchConnectionService EnsureExists()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<MatchConnectionService>();

        if (instance != null)
            return instance;

        GameObject host = new(SingletonName);
        instance = host.AddComponent<MatchConnectionService>();
        return instance;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public async Task ConnectAsync(MatchServerStatus status, CancellationToken cancellationToken = default)
    {
        if (status == null || !status.IsReady)
            throw new InvalidOperationException("Dedicated server is not ready.");

        float startTime = Time.realtimeSinceStartup;
        Debug.LogFormat(
            "[MatchConnectionService] Connect begin matchId={0} allocationId={1} endpoint={2}:{3}/{4}",
            status.matchId,
            status.allocationId,
            status.host,
            status.port,
            status.protocol
        );

        await EnsureGameSceneLoadedAsync(cancellationToken);
        LogElapsed("GameScene ready", startTime);

        NetworkManager manager = await WaitForNetworkManagerAsync(cancellationToken);
        LogElapsed("NetworkManager ready", startTime);
        UDPTransport transport = manager.transport as UDPTransport;

        if (transport == null)
            throw new InvalidOperationException("GameScene NetworkManager must use PurrNet UDPTransport for Phase 5A.");

        if (manager.clientState != ConnectionState.Disconnected)
        {
            manager.StopClient();
            await WaitForClientDisconnectedAsync(manager, cancellationToken);
        }

        ConfigureAuthenticator(manager, status);

        transport.address = status.host;
        transport.serverPort = (ushort)status.port;
        Debug.LogFormat(
            "[MatchConnectionService] Starting PurrNet client endpoint={0}:{1} previousState={2}",
            transport.address,
            transport.serverPort,
            manager.clientState
        );
        manager.StartClient();

        await WaitForClientConnectedAsync(manager, cancellationToken);
        LogElapsed("PurrNet client connected", startTime);
    }

    static void ConfigureAuthenticator(NetworkManager manager, MatchServerStatus status)
    {
        AuthService authService = AuthService.GetOrCreate();
        string userId = authService.Session?.UserId;

        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("Cannot connect to match server without an authenticated Nakama user.");

        BommyPurrNetMatchAuthenticator.ConfigureClientPayload(status.matchId, status.allocationId, userId);
        BommyPurrNetMatchAuthenticator.EnsureInstalled(manager);

        Debug.LogFormat(
            "[MatchConnectionService] Configured PurrNet auth userId={0} matchId={1} allocationId={2}",
            userId,
            status.matchId,
            status.allocationId
        );
    }

    async Task WaitForClientDisconnectedAsync(NetworkManager manager, CancellationToken cancellationToken)
    {
        float deadline = Time.realtimeSinceStartup + 5f;

        while (Time.realtimeSinceStartup < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (manager.clientState == ConnectionState.Disconnected)
                return;

            await Task.Yield();
        }

        throw new TimeoutException("Timed out disconnecting previous match client.");
    }

    public Task DisconnectAsync()
    {
        if (NetworkManager.main != null && NetworkManager.main.clientState != ConnectionState.Disconnected)
            NetworkManager.main.StopClient();

        BommyPurrNetMatchAuthenticator.ClearClientPayload();
        return Task.CompletedTask;
    }

    async Task EnsureGameSceneLoadedAsync(CancellationToken cancellationToken)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.name == gameSceneName)
            return;

        float sceneStart = Time.realtimeSinceStartup;
        Debug.Log("[MatchConnectionService] Loading " + gameSceneName + " before connecting to dedicated server.");
        AsyncOperation operation = SceneManager.LoadSceneAsync(gameSceneName);
        if (operation == null)
            throw new InvalidOperationException("Unable to load " + gameSceneName + ".");

        while (!operation.isDone)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
        }

        Debug.LogFormat(
            "[MatchConnectionService] Loaded {0} in {1:0.000}s.",
            gameSceneName,
            Time.realtimeSinceStartup - sceneStart
        );
    }

    static async Task<NetworkManager> WaitForNetworkManagerAsync(CancellationToken cancellationToken)
    {
        float deadline = Time.realtimeSinceStartup + 5f;

        while (Time.realtimeSinceStartup < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (NetworkManager.main != null)
                return NetworkManager.main;

            await Task.Yield();
        }

        throw new InvalidOperationException("GameScene does not contain a PurrNet NetworkManager.");
    }

    async Task WaitForClientConnectedAsync(NetworkManager manager, CancellationToken cancellationToken)
    {
        float deadline = Time.realtimeSinceStartup + connectTimeoutSeconds;

        while (Time.realtimeSinceStartup < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (manager.clientState == ConnectionState.Connected)
                return;

            if (manager.clientState == ConnectionState.Disconnected)
                await Task.Yield();
            else
                await Task.Yield();
        }

        throw new TimeoutException("Timed out connecting to dedicated server.");
    }

    static void LogElapsed(string step, float startTime)
    {
        Debug.LogFormat(
            "[MatchConnectionService] {0} after {1:0.000}s.",
            step,
            Time.realtimeSinceStartup - startTime
        );
    }
}
