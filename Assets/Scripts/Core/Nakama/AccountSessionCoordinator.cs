using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Governs authentication, account-data loading, scene entry, and account teardown.
/// </summary>
public sealed class AccountSessionCoordinator : MonoBehaviour
{
    const string SingletonName = "[AccountSessionCoordinator]";
    const string AuthGateSceneName = "AuthGate";
    const string MainMenuSceneName = "MainMenu";

    static AccountSessionCoordinator instance;
    bool transitionInProgress;

    public static AccountSessionCoordinator Instance => instance;
    public bool TransitionInProgress => transitionInProgress;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        EnsureExists();
    }

    public static AccountSessionCoordinator EnsureExists()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<AccountSessionCoordinator>();

        if (instance != null)
            return instance;

        GameObject host = new(SingletonName);
        instance = host.AddComponent<AccountSessionCoordinator>();
        return instance;
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        if (transitionInProgress)
            return false;

        transitionInProgress = true;

        try
        {
            NakamaConnectionManager manager = NakamaConnectionManager.EnsureExists();

            if (!await manager.TryRestoreSessionAsync())
                return false;

            await CompleteAuthenticationAsync();
            return true;
        }
        catch
        {
            PlayerProgressionService.EnsureExists().Clear();
            MatchSessionBroker.Reset();
            GameSession.Reset();
            ProfileFriendsPrototypeUI.ResetRuntimeState();
            throw;
        }
        finally
        {
            transitionInProgress = false;
        }
    }

    public async Task SignInAsync(string email, string password)
    {
        await RunAuthenticationAsync(() => NakamaConnectionManager.EnsureExists().SignInAsync(email, password));
    }

    public async Task RegisterAsync(string email, string username, string password)
    {
        await RunAuthenticationAsync(() => NakamaConnectionManager.EnsureExists().RegisterAsync(email, username, password));
    }

    public async Task LogoutAsync()
    {
        await TeardownAsync(invalidateServerSession: true);
        LoadGate();
    }

    public async Task SwitchAccountAsync()
    {
        await LogoutAsync();
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
        NakamaConnectionManager.EnsureExists();
        PlayerProgressionService.EnsureExists();
        SceneManager.sceneLoaded += GuardAuthenticatedScene;
    }

    void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= GuardAuthenticatedScene;
    }

    async Task RunAuthenticationAsync(Func<Task<Nakama.ISession>> authenticate)
    {
        if (transitionInProgress)
            throw new InvalidOperationException("An account transition is already in progress.");

        transitionInProgress = true;

        try
        {
            await authenticate();
            await CompleteAuthenticationAsync();
        }
        catch
        {
            await TeardownAsync(invalidateServerSession: false);
            throw;
        }
        finally
        {
            transitionInProgress = false;
        }
    }

    async Task CompleteAuthenticationAsync()
    {
        NakamaConnectionManager manager = NakamaConnectionManager.EnsureExists();
        await PlayerProgressionService.EnsureExists().RefreshAsync();
        await manager.ConnectSocketAsync();
        SceneManager.LoadScene(MainMenuSceneName);
    }

    async Task TeardownAsync(bool invalidateServerSession)
    {
        NakamaConnectionManager manager = NakamaConnectionManager.EnsureExists();

        try
        {
            if (invalidateServerSession)
                await manager.LogoutAsync();
            else
            {
                await manager.DisconnectSocketAsync();
                manager.ClearLocalSession();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[AccountSessionCoordinator] Session teardown failed: {exception.Message}", this);
            manager.ClearLocalSession();
        }

        PlayerProgressionService.EnsureExists().Clear();
        MatchSessionBroker.Reset();
        GameSession.Reset();
        ProfileFriendsPrototypeUI.ResetRuntimeState();
    }

    static void LoadGate()
    {
        if (SceneManager.GetActiveScene().name != AuthGateSceneName)
            SceneManager.LoadScene(AuthGateSceneName);
    }

    void GuardAuthenticatedScene(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == AuthGateSceneName || transitionInProgress)
            return;

        if (!NakamaConnectionManager.EnsureExists().IsAuthenticated)
            LoadGate();
    }
}
