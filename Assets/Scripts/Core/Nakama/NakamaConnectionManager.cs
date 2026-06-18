using System;
using System.Threading;
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
/// Persistent owner of Nakama client, email session, account, and socket state.
/// Authentication flow and scene entry are coordinated by AccountSessionCoordinator.
/// </summary>
public sealed class NakamaConnectionManager : MonoBehaviour
{
    const string SingletonName = "[NakamaConnectionManager]";
    const string AuthTokenPrefName = "Bommy.Nakama.AuthToken";
    const string RefreshTokenPrefName = "Bommy.Nakama.RefreshToken";
    const string LegacySessionPrefName = "Bommy.Nakama.Session";
    const string LegacyDeviceIdPrefName = "Bommy.Nakama.DeviceId";
    const string LegacyDisplayNameCachePrefName = "Bommy.Nakama.DisplayNameCache";
    const string DisplayNameCachePrefix = "Bommy.Nakama.DisplayNameCache.";
    const float RetryInitialDelaySeconds = 1f;
    const float RetryMaxDelaySeconds = 5f;

    static NakamaConnectionManager instance;
    bool socketConnected;
    bool applicationQuitting;
    bool manualDisconnectRequested;
    CancellationTokenSource reconnectCancellation;

    [Header("Server")]
    [SerializeField] private string scheme = "http";
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 7350;
    [SerializeField] private string serverKey = "defaultkey";

    public static event Action StatusChanged;
    public static event Action AccountChanged;

    public static NakamaConnectionManager Instance => instance;
    public IClient Client { get; private set; }
    public ISession Session { get; private set; }
    public ISocket Socket { get; private set; }
    public IApiAccount Account { get; private set; }
    public NakamaConnectionStatus Status { get; private set; } = NakamaConnectionStatus.Uninitialized;
    public string DisplayName { get; private set; } = "Player";
    public string Username => Account?.User?.Username ?? Session?.Username ?? string.Empty;

    public bool IsAuthenticated =>
        Session != null &&
        !Session.HasExpired(DateTime.UtcNow) &&
        Account != null &&
        !string.IsNullOrWhiteSpace(Account.Email);

    public bool IsServerReady =>
        Status == NakamaConnectionStatus.SocketConnected &&
        socketConnected &&
        IsAuthenticated;

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

    public async Task<ISession> SignInAsync(string email, string password)
    {
        EnsureClient();
        SetStatus(NakamaConnectionStatus.Initializing);

        try
        {
            ISession session = await Client.AuthenticateEmailAsync(
                email.Trim().ToLowerInvariant(),
                password,
                username: null,
                create: false
            );
            await AcceptEmailSessionAsync(session);
            return Session;
        }
        catch
        {
            SetStatus(NakamaConnectionStatus.Failed);
            throw;
        }
    }

    public async Task<ISession> RegisterAsync(string email, string username, string password)
    {
        EnsureClient();
        SetStatus(NakamaConnectionStatus.Initializing);

        try
        {
            string handle = username.Trim().ToLowerInvariant();
            ISession session = await Client.AuthenticateEmailAsync(
                email.Trim().ToLowerInvariant(),
                password,
                handle,
                create: true
            );

            if (!session.Created)
            {
                await Client.SessionLogoutAsync(session);
                throw new InvalidOperationException("An account already exists for that email.");
            }

            SetSession(session);
            await Client.UpdateAccountAsync(Session, handle, handle);
            await RefreshAccountAsync();
            ValidateEmailAccount();
            SetStatus(NakamaConnectionStatus.Authenticated);
            return Session;
        }
        catch
        {
            ClearAccountState(clearTokens: true);
            SetStatus(NakamaConnectionStatus.Failed);
            throw;
        }
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        EnsureClient();
        SetStatus(NakamaConnectionStatus.Initializing);

        string authToken = PlayerPrefs.GetString(AuthTokenPrefName, string.Empty);
        string refreshToken = PlayerPrefs.GetString(RefreshTokenPrefName, string.Empty);

        if (string.IsNullOrWhiteSpace(authToken))
        {
            ClearLegacyDeviceSession();
            SetStatus(NakamaConnectionStatus.Uninitialized);
            return false;
        }

        ISession restored = Nakama.Session.Restore(authToken, refreshToken);

        if (restored == null ||
            (restored.HasExpired(DateTime.UtcNow.AddMinutes(1)) &&
             (string.IsNullOrWhiteSpace(restored.RefreshToken) ||
              restored.HasRefreshExpired(DateTime.UtcNow.AddMinutes(1)))))
        {
            ClearAccountState(clearTokens: true);
            SetStatus(NakamaConnectionStatus.Uninitialized);
            return false;
        }

        try
        {
            if (restored.HasExpired(DateTime.UtcNow.AddMinutes(1)))
                restored = await Client.SessionRefreshAsync(restored);

            await AcceptEmailSessionAsync(restored);
            return true;
        }
        catch (ApiResponseException exception) when (exception.StatusCode == 401 || exception.StatusCode == 403)
        {
            Debug.LogWarning($"[NakamaConnectionManager] Saved session was rejected: {exception.Message}", this);
            ClearAccountState(clearTokens: true);
            SetStatus(NakamaConnectionStatus.Uninitialized);
            return false;
        }
        catch (InvalidOperationException exception)
        {
            Debug.LogWarning($"[NakamaConnectionManager] Saved account was rejected: {exception.Message}", this);
            ClearAccountState(clearTokens: true);
            SetStatus(NakamaConnectionStatus.Uninitialized);
            return false;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[NakamaConnectionManager] Session restore failed: {exception.Message}", this);
            ClearAccountState(clearTokens: false);
            SetStatus(NakamaConnectionStatus.Failed);
            throw;
        }
    }

    public async Task<IApiAccount> RefreshAccountAsync()
    {
        ISession session = Account == null
            ? RequireSession(allowAccountPending: true)
            : await RequireFreshSessionAsync();
        Account = await Client.GetAccountAsync(session);
        RefreshDisplayName();
        CacheServerDisplayName();
        AccountChanged?.Invoke();
        StatusChanged?.Invoke();
        return Account;
    }

    public async Task<IApiAccount> UpdateDisplayNameAsync(string displayName)
    {
        ISession session = await RequireFreshSessionAsync();
        await Client.UpdateAccountAsync(session, Username, displayName.Trim());
        return await RefreshAccountAsync();
    }

    public async Task<IApiFriendList> ListFriendsAsync()
    {
        return await Client.ListFriendsAsync(await RequireFreshSessionAsync(), state: null, limit: 100);
    }

    public async Task AddFriendAsync(string identifier)
    {
        ISession session = await RequireFreshSessionAsync();
        string value = identifier.Trim();

        if (Guid.TryParse(value, out _))
            await Client.AddFriendsAsync(session, new[] { value });
        else
            await Client.AddFriendsAsync(session, Array.Empty<string>(), new[] { value });
    }

    public async Task AcceptFriendAsync(string userId)
    {
        await Client.AddFriendsAsync(await RequireFreshSessionAsync(), new[] { userId });
    }

    public async Task DeleteFriendAsync(string userId)
    {
        await Client.DeleteFriendsAsync(await RequireFreshSessionAsync(), new[] { userId });
    }

    public async Task<IApiRpc> RpcAsync(string id, string payload = "{}")
    {
        return await Client.RpcAsync(await RequireFreshSessionAsync(), id, payload);
    }

    public async Task ConnectSocketAsync()
    {
        manualDisconnectRequested = false;
        CancelReconnect();
        await ConnectSocketInternalAsync();
    }

    async Task ConnectSocketInternalAsync()
    {
        await RequireFreshSessionAsync();
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

    public async Task DisconnectSocketAsync()
    {
        manualDisconnectRequested = true;
        CancelReconnect();
        socketConnected = false;

        if (Socket != null)
            await Socket.CloseAsync();

        SetStatus(IsAuthenticated
            ? NakamaConnectionStatus.Authenticated
            : NakamaConnectionStatus.Uninitialized);
    }

    public async Task LogoutAsync()
    {
        ISession session = Session;

        try
        {
            await DisconnectSocketAsync();

            if (session != null)
                await Client.SessionLogoutAsync(session);
        }
        finally
        {
            ClearAccountState(clearTokens: true);
            SetStatus(NakamaConnectionStatus.Uninitialized);
        }
    }

    public void ClearLocalSession()
    {
        manualDisconnectRequested = true;
        CancelReconnect();
        ClearAccountState(clearTokens: true);
        SetStatus(NakamaConnectionStatus.Uninitialized);
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
        EnsureClient();
        ClearLegacyDeviceSession();
        RefreshDisplayName();
    }

    async void OnApplicationQuit()
    {
        applicationQuitting = true;
        manualDisconnectRequested = true;
        CancelReconnect();

        try
        {
            if (Socket != null && socketConnected)
                await Socket.CloseAsync();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[NakamaConnectionManager] Socket close failed: {exception.Message}", this);
        }
    }

    async Task AcceptEmailSessionAsync(ISession session)
    {
        SetSession(session);
        await RefreshAccountAsync();
        ValidateEmailAccount();
        SetStatus(NakamaConnectionStatus.Authenticated);
    }

    void SetSession(ISession session)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        PlayerPrefs.SetString(AuthTokenPrefName, session.AuthToken ?? string.Empty);
        PlayerPrefs.SetString(RefreshTokenPrefName, session.RefreshToken ?? string.Empty);
        PlayerPrefs.Save();
    }

    void ValidateEmailAccount()
    {
        if (Account == null || string.IsNullOrWhiteSpace(Account.Email))
            throw new InvalidOperationException("This account is not linked to an email address.");
    }

    void EnsureClient()
    {
        if (Client == null)
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

    ISession RequireSession(bool allowAccountPending = false)
    {
        EnsureClient();

        if (Session == null || Session.HasExpired(DateTime.UtcNow.AddMinutes(1)))
            throw new InvalidOperationException("Nakama authentication is not available.");

        if (!allowAccountPending && (Account == null || string.IsNullOrWhiteSpace(Account.Email)))
            throw new InvalidOperationException("An email account is required.");

        return Session;
    }

    async Task<ISession> RequireFreshSessionAsync()
    {
        EnsureClient();
        ISession session = Session;

        if (session == null || Account == null || string.IsNullOrWhiteSpace(Account.Email))
            throw new InvalidOperationException("Nakama authentication is not available.");

        if (!session.HasExpired(DateTime.UtcNow.AddMinutes(1)))
            return session;

        if (string.IsNullOrWhiteSpace(session.RefreshToken) ||
            session.HasRefreshExpired(DateTime.UtcNow.AddMinutes(1)))
            throw new InvalidOperationException("Your session has expired. Log in again.");

        Session = await Client.SessionRefreshAsync(session);
        SetSession(Session);
        return Session;
    }

    void ClearAccountState(bool clearTokens)
    {
        string previousUserId = Account?.User?.Id ?? Session?.UserId;
        ReleaseSocket();
        Session = null;
        Account = null;
        socketConnected = false;

        if (clearTokens)
        {
            PlayerPrefs.DeleteKey(AuthTokenPrefName);
            PlayerPrefs.DeleteKey(RefreshTokenPrefName);

            if (!string.IsNullOrWhiteSpace(previousUserId))
                PlayerPrefs.DeleteKey(DisplayNameCachePrefix + previousUserId);

            PlayerPrefs.Save();
        }

        RefreshDisplayName();
        AccountChanged?.Invoke();
    }

    void ReleaseSocket()
    {
        if (Socket == null)
            return;

        Socket.Connected -= OnSocketConnected;
        Socket.Closed -= OnSocketClosed;
        Socket.ReceivedError -= OnSocketError;
        Socket = null;
    }

    void ClearLegacyDeviceSession()
    {
        PlayerPrefs.DeleteKey(LegacySessionPrefName);
        PlayerPrefs.DeleteKey(LegacyDeviceIdPrefName);
        PlayerPrefs.DeleteKey(LegacyDisplayNameCachePrefName);
        PlayerPrefs.DeleteKey("PlayerDisplayName");
        PlayerPrefs.DeleteKey("SelectedPlayerDisplayName");
        PlayerPrefs.Save();
    }

    void CacheServerDisplayName()
    {
        string userId = Account?.User?.Id;
        string serverDisplayName = Account?.User?.DisplayName;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(serverDisplayName))
            return;

        PlayerPrefs.SetString(DisplayNameCachePrefix + userId, serverDisplayName.Trim());
        PlayerPrefs.Save();
    }

    void RefreshDisplayName()
    {
        string serverDisplayName = Account?.User?.DisplayName;

        if (!string.IsNullOrWhiteSpace(serverDisplayName))
        {
            DisplayName = serverDisplayName.Trim();
            return;
        }

        string userId = Account?.User?.Id ?? Session?.UserId;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            string cached = PlayerPrefs.GetString(DisplayNameCachePrefix + userId, string.Empty);

            if (!string.IsNullOrWhiteSpace(cached))
            {
                DisplayName = cached;
                return;
            }
        }

        DisplayName = string.IsNullOrWhiteSpace(Username) ? "Player" : Username;
    }

    void OnSocketConnected()
    {
        socketConnected = true;
        CancelReconnect();
        SetStatus(NakamaConnectionStatus.SocketConnected);
    }

    void OnSocketClosed(string reason)
    {
        socketConnected = false;

        if (!applicationQuitting)
        {
            SetStatus(IsAuthenticated ? NakamaConnectionStatus.Authenticated : NakamaConnectionStatus.Uninitialized);
            StartReconnect();
        }
    }

    void OnSocketError(Exception exception)
    {
        socketConnected = false;
        Debug.LogWarning($"[NakamaConnectionManager] Socket error: {exception.Message}", this);
        SetStatus(IsAuthenticated ? NakamaConnectionStatus.Authenticated : NakamaConnectionStatus.Failed);
        StartReconnect();
    }

    void StartReconnect()
    {
        if (applicationQuitting || manualDisconnectRequested || !IsAuthenticated)
            return;

        CancelReconnect();
        reconnectCancellation = new CancellationTokenSource();
        _ = ReconnectLoopAsync(reconnectCancellation.Token);
    }

    async Task ReconnectLoopAsync(CancellationToken cancellationToken)
    {
        float delaySeconds = RetryInitialDelaySeconds;

        while (!cancellationToken.IsCancellationRequested && !applicationQuitting && !manualDisconnectRequested && !socketConnected)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                await ConnectSocketInternalAsync();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[NakamaConnectionManager] Socket reconnect failed: {exception.Message}", this);
                delaySeconds = Mathf.Min(delaySeconds * 2f, RetryMaxDelaySeconds);
            }
        }
    }

    void CancelReconnect()
    {
        if (reconnectCancellation == null)
            return;

        reconnectCancellation.Cancel();
        reconnectCancellation.Dispose();
        reconnectCancellation = null;
    }

    void SetStatus(NakamaConnectionStatus nextStatus)
    {
        RefreshDisplayName();
        Status = nextStatus;
        StatusChanged?.Invoke();
    }
}
