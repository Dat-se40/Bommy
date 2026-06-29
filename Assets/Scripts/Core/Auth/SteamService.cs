using UnityEngine;
#if !DISABLE_STEAM
using Steamworks;
using System;
#endif

public sealed class SteamService : MonoBehaviour
{
    private static SteamService instance;
    public static SteamService Instance => instance;

    public bool IsInitialized { get; private set; }

    public event System.Action<string> OnSteamJoinRequested;

#if !DISABLE_STEAM
    private HAuthTicket activeTicket = HAuthTicket.Invalid;
    private Callback<GameRichPresenceJoinRequested_t> m_GameRichPresenceJoinRequested;
#endif

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSteam();
    }

    private void InitializeSteam()
    {
#if !DISABLE_STEAM
        try
        {
            if (!Packsize.Test())
            {
                Debug.LogError("[SteamService] Packsize Test failed! Steamworks.NET will not function correctly.");
                return;
            }

            if (!DllCheck.Test())
            {
                Debug.LogError("[SteamService] DllCheck Test failed! Steamworks.NET will not function correctly.");
                return;
            }

            IsInitialized = SteamAPI.Init();
            if (IsInitialized)
            {
                Debug.LogFormat("[SteamService] Steam successfully initialized. AppID: {0}", SteamUtils.GetAppID());
                m_GameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);
            }
            else
            {
                Debug.LogWarning("[SteamService] SteamAPI.Init() failed. Is the Steam client running?");
            }
        }
        catch (Exception ex)
        {
            Debug.LogErrorFormat("[SteamService] Exception during Steam initialization: {0}", ex.Message);
        }
#else
        Debug.Log("[SteamService] Steam integration is disabled via DISABLE_STEAM compilation flag.");
#endif
    }

    private void Update()
    {
#if !DISABLE_STEAM
        if (IsInitialized)
        {
            SteamAPI.RunCallbacks();
        }
#endif
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

#if !DISABLE_STEAM
        CancelActiveTicket();

        if (IsInitialized)
        {
            SteamAPI.Shutdown();
            IsInitialized = false;
            Debug.Log("[SteamService] Steam shutdown completed.");
        }
#endif
    }

    public static SteamService GetOrCreate()
    {
        if (instance != null)
            return instance;

        SteamService existing = FindAnyObjectByType<SteamService>();
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new("SteamService");
        return go.AddComponent<SteamService>();
    }

    public string GetAuthSessionTicket()
    {
#if !DISABLE_STEAM
        if (!IsInitialized)
        {
            Debug.LogWarning("[SteamService] Cannot get auth ticket: Steam is not initialized.");
            return null;
        }

        CancelActiveTicket();

        byte[] ticketBuffer = new byte[1024];
        uint ticketSize;
        
        SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
        activeTicket = SteamUser.GetAuthSessionTicket(ticketBuffer, ticketBuffer.Length, out ticketSize, ref identity);

        if (activeTicket == HAuthTicket.Invalid)
        {
            Debug.LogError("[SteamService] Failed to retrieve Steam Auth Session Ticket.");
            return null;
        }

        byte[] ticketData = new byte[ticketSize];
        Array.Copy(ticketBuffer, ticketData, ticketSize);
        string hexTicket = BitConverter.ToString(ticketData).Replace("-", "").ToLowerInvariant();

        Debug.Log("[SteamService] Steam Auth Ticket generated successfully.");
        return hexTicket;
#else
        Debug.LogWarning("[SteamService] Steam is disabled. Returning dummy ticket.");
        return "dummy_steam_ticket";
#endif
    }

    private void CancelActiveTicket()
    {
#if !DISABLE_STEAM
        if (activeTicket != HAuthTicket.Invalid)
        {
            SteamUser.CancelAuthTicket(activeTicket);
            activeTicket = HAuthTicket.Invalid;
            Debug.Log("[SteamService] Cancelled active Steam auth ticket.");
        }
#endif
    }

#if !DISABLE_STEAM
    private void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t pCallback)
    {
        Debug.LogFormat("[SteamService] Rich presence join requested: {0}", pCallback.m_rgchConnect);
        OnSteamJoinRequested?.Invoke(pCallback.m_rgchConnect);
    }
#endif

    public void SetRichPresenceConnectString(string lobbyCode)
    {
#if !DISABLE_STEAM
        if (IsInitialized)
        {
            string connectValue = "nakama_lobby:" + lobbyCode;
            bool success = SteamFriends.SetRichPresence("connect", connectValue);
            Debug.LogFormat("[SteamService] Set connect rich presence to '{0}'. Success: {1}", connectValue, success);
        }
#endif
    }

    public void ClearRichPresenceConnectString()
    {
#if !DISABLE_STEAM
        if (IsInitialized)
        {
            bool success = SteamFriends.SetRichPresence("connect", null);
            Debug.LogFormat("[SteamService] Cleared connect rich presence. Success: {0}", success);
        }
#endif
    }

    public string GetLaunchCommandLineConnectString()
    {
#if !DISABLE_STEAM
        if (IsInitialized)
        {
            if (SteamApps.GetLaunchCommandLine(out string launchCmd, 1024) > 0)
            {
                Debug.LogFormat("[SteamService] Found Steam launch command line: {0}", launchCmd);
                return launchCmd;
            }
        }
#endif
        // Fallback to standard command-line arguments
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("+connect_lobby", StringComparison.OrdinalIgnoreCase))
            {
                string connectVal = args[i + 1];
                Debug.LogFormat("[SteamService] Found +connect_lobby fallback argument: {0}", connectVal);
                return connectVal;
            }
        }
        return null;
    }
}
