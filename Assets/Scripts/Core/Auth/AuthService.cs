using Nakama;
using System.Threading.Tasks;
using UnityEngine;

public sealed class AuthService : MonoBehaviour
{
    static AuthService instance;

    [Header("Nakama")]
    [SerializeField] private string scheme = "http";
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 7350;
    [SerializeField] private string serverKey = "defaultkey";
    [SerializeField] private string httpKey = "defaulthttpkey";

    NakamaSessionService sessionService;
    NakamaAccountService accountService;
    NakamaSocketService socketService;

    public IClient Client { get; private set; }
    public ISession Session => sessionService?.Session;
    public ISocket Socket => socketService?.Socket;
    public IApiAccount Account => accountService?.Account;

    public static AuthService Instance => instance;
    public bool IsSocketConnected => socketService != null && socketService.IsConnected;
    public string Username => Account?.User?.Username ?? Session?.Username ?? string.Empty;
    public string DisplayName => accountService?.GetDisplayName(Session) ?? "Player";
    public bool IsAuthenticated => Session != null && !Session.IsExpired;
    string RuntimeHttpKey => string.IsNullOrWhiteSpace(httpKey) ? "defaulthttpkey" : httpKey.Trim();

    void EnsureClient()
    {
        if (Client != null)
            return;

        string runtimeScheme = string.IsNullOrWhiteSpace(scheme) ? "http" : scheme.Trim();
        string runtimeHost = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim();

        Debug.LogFormat(
            "[AuthService] Creating Nakama client at {0}://{1}:{2}. Server key configured: {3}. HTTP key configured: {4}.",
            runtimeScheme,
            runtimeHost,
            port,
            !string.IsNullOrWhiteSpace(serverKey),
            !string.IsNullOrWhiteSpace(httpKey)
        );

        Client = new Client(runtimeScheme, runtimeHost, port, serverKey, UnityWebRequestAdapter.Instance);
    }

    void EnsureServices()
    {
        EnsureClient();
        sessionService ??= new NakamaSessionService(Client);
        accountService ??= new NakamaAccountService(Client);
        socketService ??= new NakamaSocketService(Client);
    }

    private void Awake()
    {
        if (DedicatedServerBootstrap.IsDedicatedServerRuntime)
        {
            Debug.Log("[AuthService] Dedicated server runtime detected. Skipping client auth service.");
            Destroy(gameObject);
            return;
        }

        if (instance != null && instance != this)
        {
            Debug.LogWarning("[AuthService] Duplicate AuthService found. Keeping the existing persistent instance.");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureServices();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    /// <summary>
    /// Creates and returns the AuthService instance if one doesn't already exist.
    /// Call this from MainMenu on startup.
    /// </summary>
    public static AuthService GetOrCreate()
    {
        if (DedicatedServerBootstrap.IsDedicatedServerRuntime)
            throw new System.InvalidOperationException("AuthService is client-only and cannot be used in dedicated server runtime.");

        if (instance != null)
            return instance;

        AuthService existing = FindAnyObjectByType<AuthService>();

        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new("AuthService");
        return go.AddComponent<AuthService>();
    }

    public async Task<AuthResult> TryRestoreSessionAsync()
    {
        EnsureServices();

        AuthResult result = await sessionService.TryRestoreSessionAsync();

        if (!result.Success)
            return result;

        try
        {
            await RefreshAccountAsync();
            Debug.LogFormat("Restored session for user {0}, expires at {1}", Session.UserId, Session.ExpireTime);
            return AuthResult.Ok();
        }
        catch (System.Exception ex)
        {
            Debug.LogFormat("Session restore failed: {0}", ex.Message);
            return AuthResult.Fail(AuthErrorMapper.Map(ex));
        }
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        EnsureServices();

        AuthResult result = await sessionService.LoginEmailAsync(email, password);

        if (!result.Success)
            return result;

        try
        {
            await RefreshAccountAsync();
            Debug.LogFormat("Logged in as user {0}, expires at {1}", Session.UserId, Session.ExpireTime);
            return AuthResult.Ok();
        }
        catch (System.Exception ex)
        {
            Debug.LogFormat("Login failed: {0}", ex.Message);
            return AuthResult.Fail(AuthErrorMapper.Map(ex));
        }
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string userName)
    {
        EnsureServices();

        AuthResult result = await sessionService.RegisterEmailAsync(email, password, userName);

        if (!result.Success)
            return result;

        try
        {
            await accountService.UpdateUsernameAsync(Session, userName);
            Debug.LogFormat("Registered new user {0}, expires at {1}", Session.UserId, Session.ExpireTime);
            return AuthResult.Ok();
        }
        catch (System.Exception ex)
        {
            Debug.LogFormat("Registration failed: {0}", ex.Message);
            return AuthResult.Fail(AuthErrorMapper.Map(ex));
        }
    }

    public async Task<AuthResult> LoginGuestAsync()
    {
        EnsureServices();

        AuthResult result = await sessionService.LoginGuestAsync();

        if (!result.Success)
            return result;

        try
        {
            await RefreshAccountAsync();
            Debug.LogFormat("Logged in as guest user {0}, expires at {1}", Session.UserId, Session.ExpireTime);
            return AuthResult.Ok();
        }
        catch (System.Exception ex)
        {
            Debug.LogFormat("Guest login failed: {0}", ex.Message);
            return AuthResult.Fail(AuthErrorMapper.Map(ex));
        }
    }

    public async Task<AuthResult> LoginSteamAsync(string steamToken)
    {
        EnsureServices();

        AuthResult result = await sessionService.LoginSteamAsync(steamToken);

        if (!result.Success)
            return result;

        try
        {
            await RefreshAccountAsync();
            return AuthResult.Ok();
        }
        catch (System.Exception ex)
        {
            return AuthResult.Fail(AuthErrorMapper.Map(ex));
        }
    }

    public async Task<AuthResult> UpdateDisplayNameAsync(string displayName)
    {
        EnsureServices();

        try
        {
            await accountService.UpdateDisplayNameAsync(await RequireFreshSessionAsync(), displayName);
            Debug.LogFormat("Updated display name for user {0} to {1}", Username, displayName);
            return AuthResult.Ok();
        }
        catch (System.Exception ex)
        {
            return AuthResult.Fail(AuthErrorMapper.Map(ex));
        }
    }

    public async Task LogoutAsync()
    {
        EnsureServices();

        try
        {
            await socketService.DisconnectAsync();
            await sessionService.LogoutAsync();
        }
        catch
        {
            // Best effort: local state is still cleared below.
        }
        finally
        {
            socketService.Release();
            accountService.Clear();
            sessionService.ClearLocalSession();
            Destroy(gameObject);
        }
    }

    public void ClearLocalSession()
    {
        EnsureServices();
        socketService.CloseAndRelease();
        accountService.Clear();
        sessionService.ClearLocalSession();
    }

    public async Task<IApiAccount> RefreshAccountAsync()
    {
        EnsureServices();
        return await accountService.RefreshAsync(await sessionService.RequireFreshSessionAsync());
    }

    public ISocket EnsureSocket()
    {
        EnsureServices();
        return socketService.EnsureSocket();
    }

    public async Task<ISocket> ConnectSocketAsync()
    {
        EnsureServices();
        return await socketService.ConnectAsync(await RequireFreshSessionAsync());
    }

    public async Task DisconnectSocketAsync()
    {
        EnsureServices();
        await socketService.DisconnectAsync();
    }

    public async Task<ISession> RequireFreshSessionAsync(bool requireAccount = true)
    {
        EnsureServices();

        ISession session = await sessionService.RequireFreshSessionAsync();

        if (requireAccount && accountService.Account == null)
            await accountService.RefreshAsync(session);

        return session;
    }

    public async Task<IApiRpc> RpcAsync(string id, string payload = "{}")
    {
        EnsureServices();
        return await Client.RpcAsync(await RequireFreshSessionAsync(), id, payload);
    }

    public async Task<IApiRpc> RpcUnauthenticatedAsync(string id, string payload = "{}")
    {
        EnsureServices();
        return await Client.RpcAsync(RuntimeHttpKey, id, payload);
    }
}
