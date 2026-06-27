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

        await EnsureGameSceneLoadedAsync(cancellationToken);

        NetworkManager manager = await WaitForNetworkManagerAsync(cancellationToken);
        UDPTransport transport = manager.transport as UDPTransport;

        if (transport == null)
            throw new InvalidOperationException("GameScene NetworkManager must use PurrNet UDPTransport for Phase 5A.");

        if (manager.clientState != ConnectionState.Disconnected)
            manager.StopClient();

        transport.address = status.host;
        transport.serverPort = (ushort)status.port;
        manager.StartClient();

        await WaitForClientConnectedAsync(manager, cancellationToken);
    }

    public Task DisconnectAsync()
    {
        if (NetworkManager.main != null && NetworkManager.main.clientState != ConnectionState.Disconnected)
            NetworkManager.main.StopClient();

        return Task.CompletedTask;
    }

    async Task EnsureGameSceneLoadedAsync(CancellationToken cancellationToken)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.name == gameSceneName)
            return;

        AsyncOperation operation = SceneManager.LoadSceneAsync(gameSceneName);
        if (operation == null)
            throw new InvalidOperationException("Unable to load " + gameSceneName + ".");

        while (!operation.isDone)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
        }
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
}
