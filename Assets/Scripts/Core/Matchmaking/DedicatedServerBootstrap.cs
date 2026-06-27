using System;
using System.Threading;
using System.Threading.Tasks;
using Nakama;
using PurrNet;
using PurrNet.Transports;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class DedicatedServerRpcRequest
{
    public string serverId;
    public string allocationId;
    public string matchId;
    public string host;
    public int port = 5000;
    public string protocol = "UDP";
    public string status;
    public string serverSecret;
    public string reason;
}

[Serializable]
public class MatchLaunchConfig
{
    public bool success;
    public string errorMessage;
    public string allocationId;
    public string matchId;
    public string source;
    public string roomId;
    public int mapId;
    public string mapName;
    public string region;
    public int maxPlayers;
    public MatchLaunchPlayer[] players;
    public string nakamaHost;
    public int nakamaPort;
    public int purrnetPort;

    public int IntendedPlayerCount
    {
        get
        {
            if (players != null && players.Length > 0)
                return players.Length;

            return Mathf.Max(0, maxPlayers);
        }
    }
}

[Serializable]
public class MatchLaunchPlayer
{
    public string userId;
    public string username;
    public string displayName;
    public int selectedCharacterId;
}

[Serializable]
public class EdgegapPortsMapping
{
    public EdgegapPortMap ports;
}

[Serializable]
public class EdgegapPortMap
{
    public EdgegapPort gameport;
}

[Serializable]
public class EdgegapPort
{
    public string name;
    public int internalPort;
    public int external;
    public string protocol;
}

public sealed class DedicatedServerBootstrap : MonoBehaviour
{
    [SerializeField] private bool forceDedicatedServer;
    [SerializeField] private string defaultServerId = "local-001";
    [SerializeField] private string defaultPublicHost = "127.0.0.1";
    [SerializeField] private int defaultPort = 5000;
    [SerializeField] private string defaultServerSecret = "dev-local-secret";
    [SerializeField] private float heartbeatSeconds = 5f;
    [SerializeField] private float claimRetrySeconds = 2f;

    CancellationTokenSource lifetime;
    DedicatedServerRuntimeConfig config;
    IClient serverClient;
    string currentAllocationId;
    string currentMatchId;
    bool readyMarked;
    bool registrationConfirmed;
    bool reloadingForNextMatch;

    public static bool IsDedicatedServerRuntime =>
        Application.isBatchMode
        || string.Equals(Environment.GetEnvironmentVariable("BOMMY_DEDICATED_SERVER"), "1", StringComparison.Ordinal);

    void Start()
    {
        if (!ShouldRunDedicatedServer())
            return;

        DedicatedMatchRuntime.MatchLifecycleReleased += OnMatchLifecycleReleased;
        lifetime = new CancellationTokenSource();
        config = DedicatedServerRuntimeConfig.Read(defaultServerId, defaultPublicHost, defaultPort, defaultServerSecret);
        currentAllocationId = config.allocationId;
        currentMatchId = config.matchId;
        registrationConfirmed = !string.IsNullOrWhiteSpace(currentAllocationId) || !string.IsNullOrWhiteSpace(currentMatchId);
        LogDedicatedRuntimeStart();
        _ = RunServerAsync(lifetime.Token);
    }

    void OnDestroy()
    {
        DedicatedMatchRuntime.MatchLifecycleReleased -= OnMatchLifecycleReleased;
        lifetime?.Cancel();
        lifetime?.Dispose();
        lifetime = null;
    }

    void OnMatchLifecycleReleased()
    {
        currentAllocationId = string.Empty;
        currentMatchId = string.Empty;
        readyMarked = false;
        registrationConfirmed = true;

        if (reloadingForNextMatch)
            return;

        reloadingForNextMatch = true;
        Debug.Log("[DedicatedServerBootstrap] Match lifecycle released. Reloading GameScene for next allocation.", this);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    bool ShouldRunDedicatedServer()
    {
        return forceDedicatedServer
            || IsDedicatedServerRuntime;
    }

    void LogDedicatedRuntimeStart()
    {
        Debug.LogFormat(
            this,
            "[DedicatedServerBootstrap] Dedicated runtime starting. batchMode={0} envDedicated={1} forceDedicated={2} serverId={3} provider={4} bindPort={5} public={6}:{7} nakama={8}:{9} allocationId={10} matchId={11}",
            Application.isBatchMode,
            Environment.GetEnvironmentVariable("BOMMY_DEDICATED_SERVER") ?? "",
            forceDedicatedServer,
            config.serverId,
            config.provider,
            config.bindPort,
            config.publicHost,
            config.publicPort,
            config.nakamaScheme + "://" + config.nakamaHost,
            config.nakamaPort,
            string.IsNullOrWhiteSpace(config.allocationId) ? "-" : config.allocationId,
            string.IsNullOrWhiteSpace(config.matchId) ? "-" : config.matchId
        );
    }

    async Task RunServerAsync(CancellationToken cancellationToken)
    {
        try
        {
            StartPurrNetServer();
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            Debug.LogError("[DedicatedServerBootstrap] Failed to start PurrNet server: " + exception.Message, this);
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                EnsureServerClient();

                if (!registrationConfirmed && string.IsNullOrWhiteSpace(currentAllocationId) && !readyMarked)
                    await RegisterServerAsync(cancellationToken);

                await ClaimAndReadyIfAssignedAsync(cancellationToken);
                await SendHeartbeatAsync(cancellationToken);
                float delaySeconds = readyMarked ? heartbeatSeconds : claimRetrySeconds;
                await Task.Delay(TimeSpan.FromSeconds(Mathf.Max(0.25f, delaySeconds)), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[DedicatedServerBootstrap] Backend sync failed; retrying registration. " + exception.Message, this);
                ResetBackendRegistration();
                await DelayRetryAsync(cancellationToken);
            }
        }
    }

    void EnsureServerClient()
    {
        serverClient ??= new Client(config.nakamaScheme, config.nakamaHost, config.nakamaPort, config.nakamaServerKey, UnityWebRequestAdapter.Instance);
    }

    void ResetBackendRegistration()
    {
        serverClient = null;
        currentAllocationId = string.Empty;
        currentMatchId = string.Empty;
        readyMarked = false;
        registrationConfirmed = false;
    }

    async Task DelayRetryAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(claimRetrySeconds), cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    void StartPurrNetServer()
    {
        NetworkManager manager = NetworkManager.main ?? FindAnyObjectByType<NetworkManager>();
        if (manager == null)
            throw new InvalidOperationException("Dedicated server scene does not contain a PurrNet NetworkManager.");

        UDPTransport transport = manager.transport as UDPTransport;
        if (transport == null)
            throw new InvalidOperationException("Dedicated server requires PurrNet UDPTransport for Phase 5A.");

        transport.serverPort = (ushort)config.bindPort;

        if (manager.serverState == ConnectionState.Disconnected)
            manager.StartServer();
    }

    async Task RegisterServerAsync(CancellationToken cancellationToken)
    {
        MatchServerAllocation allocation = await CallServerRpcAsync<MatchServerAllocation>(
            "register_match_server",
            BuildRequestJson("Available")
        );

        if (!allocation.success && !string.IsNullOrWhiteSpace(allocation.errorMessage))
            Debug.LogWarning("[DedicatedServerBootstrap] Register returned: " + allocation.errorMessage, this);

        registrationConfirmed = allocation.success;

        if (!string.IsNullOrWhiteSpace(allocation.allocationId))
        {
            currentAllocationId = allocation.allocationId;
            currentMatchId = allocation.matchId;
        }
    }

    async Task ClaimAndReadyIfAssignedAsync(CancellationToken cancellationToken)
    {
        if (readyMarked)
            return;

        MatchLaunchConfig launchConfig = await CallServerRpcAsync<MatchLaunchConfig>(
            "claim_match_launch_config",
            BuildRequestJson("Launching")
        );

        if (!launchConfig.success)
        {
            if (string.Equals(launchConfig.errorMessage, "Server is not registered.", StringComparison.OrdinalIgnoreCase))
                registrationConfirmed = false;

            return;
        }

        currentAllocationId = launchConfig.allocationId;
        currentMatchId = launchConfig.matchId;
        Debug.LogFormat(
            "[DedicatedServerBootstrap] Launch configuration claimed. Match ID: {0}, Map ID: {1}, Map Name: {2}, Players Count: {3}",
            launchConfig.matchId,
            launchConfig.mapId,
            launchConfig.mapName,
            launchConfig.players?.Length ?? 0
        );
        DedicatedMatchRuntime.Configure(
            launchConfig,
            serverClient,
            config.nakamaHttpKey,
            config.serverId,
            config.serverSecret,
            config.provider
        );

        MatchServerStatus ready = await CallServerRpcAsync<MatchServerStatus>(
            "mark_match_server_ready",
            BuildRequestJson("Ready")
        );

        if (!ready.success)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(ready.errorMessage)
                ? "Failed to mark dedicated server ready."
                : ready.errorMessage);

        readyMarked = true;
        Debug.Log("[DedicatedServerBootstrap] Ready for match " + currentMatchId + " on " + config.publicHost + ":" + config.publicPort, this);
    }

    async Task SendHeartbeatAsync(CancellationToken cancellationToken)
    {
        await CallServerRpcAsync<MatchServerStatus>(
            "server_heartbeat",
            BuildRequestJson(readyMarked ? "Ready" : "Available")
        );
    }

    string BuildRequestJson(string status)
    {
        return JsonUtility.ToJson(new DedicatedServerRpcRequest
        {
            serverId = config.serverId,
            allocationId = currentAllocationId,
            matchId = currentMatchId,
            host = config.publicHost,
            port = config.publicPort,
            protocol = "UDP",
            status = status,
            serverSecret = config.serverSecret
        });
    }

    async Task<T> CallServerRpcAsync<T>(string rpcId, string payload)
    {
        IApiRpc response = await serverClient.RpcAsync(config.nakamaHttpKey, rpcId, payload);
        T result = JsonUtility.FromJson<T>(response.Payload);

        if (result == null)
            throw new InvalidOperationException("Nakama returned invalid " + rpcId + " data.");

        return result;
    }

    struct DedicatedServerRuntimeConfig
    {
        public string serverId;
        public string publicHost;
        public int bindPort;
        public int publicPort;
        public string serverSecret;
        public string nakamaHost;
        public string nakamaScheme;
        public int nakamaPort;
        public string nakamaServerKey;
        public string nakamaHttpKey;
        public string provider;
        public string allocationId;
        public string matchId;

        public static DedicatedServerRuntimeConfig Read(string defaultServerId, string defaultPublicHost, int defaultPort, string defaultServerSecret)
        {
            int bindPort = ReadInt(
                "ARBITRIUM_PORT_GAMEPORT_INTERNAL",
                ReadInt("BOMMY_PURRNET_PORT", defaultPort)
            );
            int publicPort = ReadInt(
                "ARBITRIUM_PORT_GAMEPORT_EXTERNAL",
                ReadEdgegapExternalPort(bindPort)
            );

            return new DedicatedServerRuntimeConfig
            {
                serverId = ReadString("BOMMY_SERVER_ID", ReadString("ARBITRIUM_REQUEST_ID", defaultServerId)),
                publicHost = ReadString(
                    "BOMMY_PURRNET_PUBLIC_HOST",
                    ReadString("ARBITRIUM_PUBLIC_IP", ReadString("BOMMY_PURRNET_HOST", defaultPublicHost))
                ),
                bindPort = bindPort,
                publicPort = publicPort > 0 ? publicPort : bindPort,
                serverSecret = ReadString("BOMMY_SERVER_SECRET", defaultServerSecret),
                nakamaScheme = ReadString("BOMMY_NAKAMA_SCHEME", "http").ToLowerInvariant(),
                nakamaHost = ReadString("BOMMY_NAKAMA_HOST", "127.0.0.1"),
                nakamaPort = ReadInt("BOMMY_NAKAMA_PORT", 7350),
                nakamaServerKey = ReadString("BOMMY_NAKAMA_SERVER_KEY", "defaultkey"),
                nakamaHttpKey = ReadString("BOMMY_NAKAMA_HTTP_KEY", "defaulthttpkey"),
                provider = ReadString("BOMMY_PROVIDER", "LocalDev"),
                allocationId = ReadString("BOMMY_ALLOCATION_ID", ""),
                matchId = ReadString("BOMMY_MATCH_ID", "")
            };
        }

        static string ReadString(string key, string fallback)
        {
            string value = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        static int ReadInt(string key, int fallback)
        {
            string value = Environment.GetEnvironmentVariable(key);
            return int.TryParse(value, out int parsed) && parsed > 0 ? parsed : fallback;
        }

        static int ReadEdgegapExternalPort(int fallback)
        {
            string raw = Environment.GetEnvironmentVariable("ARBITRIUM_PORTS_MAPPING");
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            try
            {
                EdgegapPortsMapping mapping = JsonUtility.FromJson<EdgegapPortsMapping>(raw);
                if (mapping?.ports?.gameport != null && mapping.ports.gameport.external > 0)
                    return mapping.ports.gameport.external;
            }
            catch
            {
            }

            return fallback;
        }
    }
}
