using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

public enum NakamaConnectionStatus
{
    Uninitialized,
    Initializing,
    Authenticated,
    SocketConnected,
    Failed
}

/// <summary>
/// Global Unity-side Nakama client/session/socket owner.
/// </summary>
public sealed class NakamaConnectionManager : MonoBehaviour
{
    const string SingletonName = "[NakamaConnectionManager]";
    const string SessionPrefName = "Bommy.Nakama.Session";
    const string DeviceIdPrefName = "Bommy.Nakama.DeviceId";
    const string PlayerDisplayNamePrefName = "PlayerDisplayName";
    const float DefaultRetryInitialDelaySeconds = 1f;
    const float DefaultRetryMaxDelaySeconds = 5f;

    static NakamaConnectionManager instance;

    [Header("Server")]
    [SerializeField] private string scheme = "http";
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 7350;
    [SerializeField] private string serverKey = "defaultkey";

    [Header("Startup")]
    [SerializeField] private bool initializeOnAwake = true;
    [SerializeField] private bool connectSocketAfterAuth = true;

    [Header("Retry")]
    [SerializeField] private bool retryOnFailure = true;
    [SerializeField] private float retryInitialDelaySeconds = DefaultRetryInitialDelaySeconds;
    [SerializeField] private float retryMaxDelaySeconds = DefaultRetryMaxDelaySeconds;

    Task initializeTask;
    Task retryTask;
    float nextRetryDelaySeconds = DefaultRetryInitialDelaySeconds;
    int retryVersion;
    bool manualDisconnectRequested;
    bool applicationQuitting;
    bool socketConnected;

    public static event Action StatusChanged;

    public static NakamaConnectionManager Instance => instance;

    public IClient Client { get; private set; }
    public ISession Session { get; private set; }
    public ISocket Socket { get; private set; }
    public NakamaConnectionStatus Status { get; private set; } = NakamaConnectionStatus.Uninitialized;
    public string DisplayName { get; private set; } = "Player";

    public bool IsServerReady =>
        Status == NakamaConnectionStatus.SocketConnected &&
        socketConnected &&
        Session != null &&
        !Session.HasExpired(DateTime.UtcNow.AddMinutes(1));

    public static NakamaConnectionManager EnsureExists()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<NakamaConnectionManager>();

        if (instance != null)
            return instance;

        GameObject managerObject = new(SingletonName);
        instance = managerObject.AddComponent<NakamaConnectionManager>();
        return instance;
    }

    public Task InitializeAsync()
    {
        manualDisconnectRequested = false;

        if (IsServerReady)
            return Task.CompletedTask;

        if (initializeTask != null && !initializeTask.IsCompleted)
            return initializeTask;

        initializeTask = InitializeInternalAsync(scheduleRetryOnFailure: true);
        return initializeTask;
    }

    public async Task<ISession> AuthenticateDeviceAsync()
    {
        EnsureClient();
        SetStatus(NakamaConnectionStatus.Initializing);

        ISession restoredSession = TryRestoreSession();

        if (restoredSession != null)
        {
            Session = restoredSession;
            RefreshDisplayName();
            SetStatus(NakamaConnectionStatus.Authenticated);
            return Session;
        }

        string deviceId = GetOrCreateDeviceId();
        Session = await Client.AuthenticateDeviceAsync(deviceId);

        PlayerPrefs.SetString(SessionPrefName, Session.AuthToken);
        PlayerPrefs.Save();

        RefreshDisplayName();
        SetStatus(NakamaConnectionStatus.Authenticated);

        return Session;
    }

    public async Task ConnectSocketAsync()
    {
        if (Session == null || Session.HasExpired(DateTime.UtcNow.AddMinutes(1)))
            await AuthenticateDeviceAsync();

        EnsureSocket();

        if (socketConnected)
        {
            SetStatus(NakamaConnectionStatus.SocketConnected);
            return;
        }

        await Socket.ConnectAsync(Session);

        socketConnected = true;
        SetStatus(NakamaConnectionStatus.SocketConnected);
    }

    public async Task DisconnectAsync()
    {
        manualDisconnectRequested = true;
        CancelRetryLoop();
        socketConnected = false;

        if (Socket != null)
            await Socket.CloseAsync();

        if (Session != null && !Session.HasExpired(DateTime.UtcNow.AddMinutes(1)))
            SetStatus(NakamaConnectionStatus.Authenticated);
        else
            SetStatus(NakamaConnectionStatus.Uninitialized);
    }

    async void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureClient();
        RefreshDisplayName();

        if (initializeOnAwake)
            await InitializeAsync();
    }

    async void OnApplicationQuit()
    {
        applicationQuitting = true;
        manualDisconnectRequested = true;
        CancelRetryLoop();

        try
        {
            await DisconnectAsync();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[NakamaConnectionManager] Disconnect failed: {exception.Message}", this);
        }
    }

    async Task InitializeInternalAsync(bool scheduleRetryOnFailure)
    {
        try
        {
            await AuthenticateDeviceAsync();

            if (connectSocketAfterAuth)
                await ConnectSocketAsync();

            ResetRetryDelay();

            if (scheduleRetryOnFailure)
                CancelRetryLoop();
        }
        catch (Exception exception)
        {
            socketConnected = false;
            SetStatus(NakamaConnectionStatus.Failed);
            Debug.LogWarning($"[NakamaConnectionManager] Nakama startup failed: {exception.Message}", this);

            if (scheduleRetryOnFailure)
                StartRetryLoop();
        }
    }

    void StartRetryLoop()
    {
        if (!retryOnFailure || applicationQuitting || manualDisconnectRequested || IsServerReady)
            return;

        if (retryTask != null && !retryTask.IsCompleted)
            return;

        int version = ++retryVersion;
        retryTask = RetryLoopAsync(version);
    }

    async Task RetryLoopAsync(int version)
    {
        try
        {
            while (version == retryVersion && retryOnFailure && !applicationQuitting && !manualDisconnectRequested && !IsServerReady)
            {
                float retryDelaySeconds = ConsumeRetryDelay();
                Debug.LogWarning($"[NakamaConnectionManager] Retrying Nakama connection in {retryDelaySeconds:0.#}s.", this);

                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));

                if (version != retryVersion || applicationQuitting || manualDisconnectRequested || IsServerReady)
                    break;

                if (initializeTask != null && !initializeTask.IsCompleted)
                    await initializeTask;
                else
                {
                    initializeTask = InitializeInternalAsync(scheduleRetryOnFailure: false);
                    await initializeTask;
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[NakamaConnectionManager] Retry loop failed: {exception.Message}", this);
        }
        finally
        {
            if (version == retryVersion)
                retryTask = null;
        }
    }

    float ConsumeRetryDelay()
    {
        float retryDelaySeconds = Mathf.Clamp(nextRetryDelaySeconds, retryInitialDelaySeconds, retryMaxDelaySeconds);
        nextRetryDelaySeconds = Mathf.Min(retryDelaySeconds * 2f, retryMaxDelaySeconds);
        return retryDelaySeconds;
    }

    void ResetRetryDelay()
    {
        nextRetryDelaySeconds = retryInitialDelaySeconds;
    }

    void CancelRetryLoop()
    {
        retryVersion++;
        retryTask = null;
    }

    void EnsureClient()
    {
        if (Client != null)
            return;

        Client = new Client(scheme, host, port, serverKey);
    }

    void EnsureSocket()
    {
        if (Socket != null)
            return;

        Socket = Client.NewSocket(useMainThread: true);
        Socket.Connected += OnSocketConnected;
        Socket.Closed += OnSocketClosed;
        Socket.ReceivedError += OnSocketError;
    }

    ISession TryRestoreSession()
    {
        string authToken = PlayerPrefs.GetString(SessionPrefName, string.Empty);

        if (string.IsNullOrWhiteSpace(authToken))
            return null;

        ISession restoredSession = Nakama.Session.Restore(authToken);

        if (restoredSession == null || restoredSession.HasExpired(DateTime.UtcNow.AddMinutes(1)))
            return null;

        return restoredSession;
    }

    string GetOrCreateDeviceId()
    {
        string deviceId = PlayerPrefs.GetString(DeviceIdPrefName, string.Empty);

        if (!string.IsNullOrWhiteSpace(deviceId))
            return deviceId;

        deviceId = SystemInfo.deviceUniqueIdentifier;

        if (string.IsNullOrWhiteSpace(deviceId) || deviceId == SystemInfo.unsupportedIdentifier)
            deviceId = Guid.NewGuid().ToString("N");

        PlayerPrefs.SetString(DeviceIdPrefName, deviceId);
        PlayerPrefs.Save();

        return deviceId;
    }

    void RefreshDisplayName()
    {
        string playerPrefsName = PlayerPrefs.GetString(PlayerDisplayNamePrefName, string.Empty);

        if (!string.IsNullOrWhiteSpace(playerPrefsName))
        {
            DisplayName = playerPrefsName;
            return;
        }

        if (Session != null && !string.IsNullOrWhiteSpace(Session.Username))
        {
            DisplayName = Session.Username;
            return;
        }

        DisplayName = "Player";
    }

    void OnSocketConnected()
    {
        socketConnected = true;
        ResetRetryDelay();
        CancelRetryLoop();
        SetStatus(NakamaConnectionStatus.SocketConnected);
    }

    void OnSocketClosed(string reason)
    {
        socketConnected = false;

        if (Session != null && !Session.HasExpired(DateTime.UtcNow.AddMinutes(1)))
            SetStatus(NakamaConnectionStatus.Authenticated);
        else
            SetStatus(NakamaConnectionStatus.Uninitialized);

        StartRetryLoop();
    }

    void OnSocketError(Exception exception)
    {
        socketConnected = false;
        Debug.LogWarning($"[NakamaConnectionManager] Socket error: {exception.Message}", this);

        if (Session != null && !Session.HasExpired(DateTime.UtcNow.AddMinutes(1)))
            SetStatus(NakamaConnectionStatus.Authenticated);
        else
            SetStatus(NakamaConnectionStatus.Failed);

        StartRetryLoop();
    }

    void OnValidate()
    {
        retryInitialDelaySeconds = Mathf.Max(0.1f, retryInitialDelaySeconds);
        retryMaxDelaySeconds = Mathf.Max(retryInitialDelaySeconds, retryMaxDelaySeconds);
        nextRetryDelaySeconds = Mathf.Clamp(nextRetryDelaySeconds, retryInitialDelaySeconds, retryMaxDelaySeconds);
    }

    void SetStatus(NakamaConnectionStatus nextStatus)
    {
        RefreshDisplayName();

        if (Status == nextStatus)
        {
            StatusChanged?.Invoke();
            return;
        }

        Status = nextStatus;
        StatusChanged?.Invoke();
    }
}
