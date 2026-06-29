using Nakama;
using System;
using System.Threading.Tasks;
using UnityEngine;

internal sealed class NakamaSessionService
{
    readonly IClient client;

    public ISession Session { get; private set; }

    public NakamaSessionService(IClient client)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<AuthResult> TryRestoreSessionAsync()
    {
        if (!SessionStore.HasSession())
            return AuthResult.Fail(null);

        var (authToken, refreshToken, _) = SessionStore.Load();

        try
        {
            ISession restored = Nakama.Session.Restore(authToken, refreshToken);

            if (restored == null)
            {
                SessionStore.Clear();
                return AuthResult.Fail(null);
            }

            if (restored.IsExpired)
                restored = await client.SessionRefreshAsync(restored);

            SetSession(restored);
            return AuthResult.Ok();
        }
        catch (ApiResponseException ex) when (ex.StatusCode == 401)
        {
            SessionStore.Clear();
            Debug.LogFormat("Session restore failed: {0}", ex.Message);
            return AuthResult.Fail(null);
        }
        catch (Exception ex)
        {
            Debug.LogFormat("Session restore failed: {0}", ex.Message);
            return AuthResult.Fail(AuthErrorMapper.Map(ex));
        }
    }

    public async Task<AuthResult> LoginEmailAsync(string email, string password)
    {
        try
        {
            SetSession(await client.AuthenticateEmailAsync(email, password, create: false));
            return AuthResult.Ok();
        }
        catch (ApiResponseException ex) when (ex.StatusCode == 401 || ex.StatusCode == 404)
        {
            Debug.LogFormat("Login failed: {0}", ex.Message);
            return AuthResult.Fail(AuthErrorMapper.Login(ex));
        }
        catch (Exception ex)
        {
            Debug.LogFormat("Login failed: {0}", ex.Message);
            return AuthResult.Fail(AuthErrorMapper.Map(ex));
        }
    }

    public async Task<AuthResult> RegisterEmailAsync(string email, string password, string username)
    {
        try
        {
            SetSession(await client.AuthenticateEmailAsync(email, password, create: true, username: username));
            return AuthResult.Ok();
        }
        catch (ApiResponseException ex) when (ex.StatusCode == 409)
        {
            Debug.LogFormat("Registration failed: {0}", ex.Message);
            return AuthResult.Fail(AuthErrorMapper.Register(ex));
        }
        catch (Exception ex)
        {
            Debug.LogFormat("Registration failed: {0}", ex.Message);
            return AuthResult.Fail(AuthErrorMapper.Map(ex));
        }
    }

    public async Task<AuthResult> LoginGuestAsync()
    {
        try
        {
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            SetSession(await client.AuthenticateDeviceAsync(deviceId, create: true));
            return AuthResult.Ok();
        }
        catch (Exception ex)
        {
            Debug.LogFormat("Guest login failed: {0}", ex.Message);
            return AuthResult.Fail(AuthErrorMapper.Map(ex));
        }
    }

    public async Task<AuthResult> LoginSteamAsync(string steamToken)
    {
        try
        {
            SetSession(await client.AuthenticateSteamAsync(steamToken, create: true, import: true));
            return AuthResult.Ok();
        }
        catch (Exception ex)
        {
            return AuthResult.Fail(AuthErrorMapper.Map(ex));
        }
    }

    public async Task<AuthResult> LinkSteamAsync(string steamToken, bool import = true)
    {
        try
        {
            await client.LinkSteamAsync(Session, steamToken, import);
            return AuthResult.Ok();
        }
        catch (Exception ex)
        {
            return AuthResult.Fail(AuthErrorMapper.Map(ex));
        }
    }

    public async Task<AuthResult> UnlinkSteamAsync(string steamToken)
    {
        try
        {
            await client.UnlinkSteamAsync(Session, steamToken);
            return AuthResult.Ok();
        }
        catch (Exception ex)
        {
            return AuthResult.Fail(AuthErrorMapper.Map(ex));
        }
    }


    public async Task<ISession> RequireFreshSessionAsync()
    {
        ISession session = Session;

        if (session == null)
            throw new InvalidOperationException("Nakama authentication is not available.");

        if (!session.HasExpired(DateTime.UtcNow.AddMinutes(General.AUTH_SESSION_REFRESH_BUFFER_MINUTES)))
            return session;

        if (string.IsNullOrWhiteSpace(session.RefreshToken) ||
            session.HasRefreshExpired(DateTime.UtcNow.AddMinutes(General.AUTH_SESSION_REFRESH_BUFFER_MINUTES)))
            throw new InvalidOperationException("Your session has expired. Log in again.");

        SetSession(await client.SessionRefreshAsync(session));
        return Session;
    }

    public async Task LogoutAsync()
    {
        if (Session != null)
            await client.SessionLogoutAsync(Session);
    }

    public void ClearLocalSession()
    {
        Session = null;
        SessionStore.Clear();
    }

    void SetSession(ISession session)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        SessionStore.Save(Session);
    }
}
