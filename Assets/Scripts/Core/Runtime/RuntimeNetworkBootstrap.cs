using System.Collections;
using System.Reflection;
using PurrNet;
using PurrNet.Authentication;
using PurrNet.Transports;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public sealed class RuntimeNetworkBootstrap : MonoBehaviour
{
    const string DefaultGameSceneName = "GameScene";
    static readonly FieldInfo AuthenticatorField = typeof(NetworkManager).GetField(
        "_authenticator",
        BindingFlags.Instance | BindingFlags.NonPublic
    );

    static RuntimeNetworkBootstrap instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        RuntimeMode.Parse();

        if (instance != null)
            return;

        GameObject go = new GameObject(nameof(RuntimeNetworkBootstrap));
        instance = go.AddComponent<RuntimeNetworkBootstrap>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        RuntimeMode.Parse();
        SceneManager.sceneLoaded += OnSceneLoaded;
        FlowGuard.Info(FlowGuard.TagSetup, $"Runtime mode: {RuntimeMode.Describe()}", this);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (RuntimeMode.IsDedicatedServer && scene.name != DefaultGameSceneName)
        {
            SceneManager.LoadScene(DefaultGameSceneName);
            return;
        }

        if (scene.name != DefaultGameSceneName)
            return;

        NetworkManager manager = FindFirstObjectByType<NetworkManager>();

        if (manager == null)
        {
            FlowGuard.Error(FlowGuard.TagNetwork, "GameScene loaded without a PurrNet NetworkManager.", this);
            return;
        }

        if (RuntimeMode.IsDedicatedServer)
        {
            ConfigureForManualStart(manager);
            ConfigureAuthenticator(manager);
            ConfigureTransport(manager, "0.0.0.0", RuntimeMode.Port);
            StartCoroutine(StartDedicatedServer(manager));
            return;
        }

        if (MatchSessionBroker.TryGetMatchAllocation(out MatchServerAllocation allocation))
        {
            ConfigureForManualStart(manager);
            ConfigureAuthenticator(manager);
            ConfigureTransport(manager, allocation.host, allocation.port);
            StartCoroutine(StartClient(manager, allocation));
        }
    }

    IEnumerator StartDedicatedServer(NetworkManager manager)
    {
        yield return null;

        if (manager.serverState == ConnectionState.Disconnected)
        {
            FlowGuard.Info(
                FlowGuard.TagNetwork,
                $"Starting dedicated server on port {RuntimeMode.Port} matchId={RuntimeMode.MatchId ?? "-"}",
                this
            );
            manager.StartServer();
        }

        PlayerProfileApiClient apiClient = EnsureApiClient();
        apiClient.RegisterServerReady(success =>
        {
            if (!success)
                FlowGuard.Error(FlowGuard.TagRestApi, "Failed to register dedicated server ready.", this);

            DedicatedServerState.MarkReady(RuntimeMode.MatchId, RuntimeMode.EdgegapDeploymentId);
        });
    }

    IEnumerator StartClient(NetworkManager manager, MatchServerAllocation allocation)
    {
        yield return null;

        if (manager.clientState == ConnectionState.Disconnected)
        {
            FlowGuard.Info(
                FlowGuard.TagNetwork,
                $"Connecting client to {allocation.host}:{allocation.port} matchId={allocation.matchId}",
                this
            );
            manager.StartClient();
        }
    }

    static void ConfigureForManualStart(NetworkManager manager)
    {
        manager.startServerFlags = StartFlags.None;
        manager.startClientFlags = StartFlags.None;
    }

    static void ConfigureAuthenticator(NetworkManager manager)
    {
        MatchJoinAuthenticator authenticator = manager.GetComponent<MatchJoinAuthenticator>();

        if (authenticator == null)
            authenticator = manager.gameObject.AddComponent<MatchJoinAuthenticator>();

        authenticator.Configure(EnsureApiClient());

        if (AuthenticatorField == null)
        {
            FlowGuard.Error(FlowGuard.TagNetwork, "Unable to assign PurrNet authenticator field.", manager);
            return;
        }

        AuthenticatorField.SetValue(manager, authenticator as AuthenticationLayer);
    }

    static PlayerProfileApiClient EnsureApiClient()
    {
        PlayerProfileApiClient apiClient = FindAnyObjectByType<PlayerProfileApiClient>();

        if (apiClient != null)
            return apiClient;

        GameObject go = new GameObject(nameof(PlayerProfileApiClient));
        DontDestroyOnLoad(go);
        return go.AddComponent<PlayerProfileApiClient>();
    }

    static void ConfigureTransport(NetworkManager manager, string host, int port)
    {
        ushort safePort = (ushort)Mathf.Clamp(port, 1, ushort.MaxValue);
        GenericTransport transport = manager.GetComponent<GenericTransport>();

        if (transport is UDPTransport udp)
        {
            udp.serverPort = safePort;
            udp.address = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host;
            return;
        }

        if (transport is WebTransport web)
        {
            web.serverPort = safePort;
            web.address = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host;
            return;
        }

        FlowGuard.Info(
            FlowGuard.TagNetwork,
            $"Transport {transport?.GetType().Name ?? "-"} does not expose address/port configuration.",
            manager
        );
    }
}
