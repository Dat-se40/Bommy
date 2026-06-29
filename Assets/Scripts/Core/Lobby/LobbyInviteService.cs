using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LobbyInviteService : MonoBehaviour
{
    public const int LobbyInviteNotificationCode = 1001;

    const string SingletonName = "[LobbyInviteService]";
    const string LobbySceneName = "Lobby";

    static LobbyInviteService instance;

    ISocket boundSocket;
    bool binding;

    public static LobbyInviteService Instance => instance;

    public static LobbyInviteService EnsureExists()
    {
        if (instance != null)
        {
            instance.EnsureSocketBinding();
            return instance;
        }

        instance = FindAnyObjectByType<LobbyInviteService>();

        if (instance != null)
        {
            instance.EnsureSocketBinding();
            return instance;
        }

        GameObject host = new(SingletonName);
        instance = host.AddComponent<LobbyInviteService>();
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
        EnsureSocketBinding();
    }

    void OnDestroy()
    {
        UnbindSocket();

        if (instance == this)
            instance = null;
    }

    public void EnsureSocketBinding()
    {
        if (binding)
            return;

        _ = BindSocketAsync();
    }

    async Task BindSocketAsync()
    {
        binding = true;

        try
        {
            ISocket socket = await AuthService.GetOrCreate().ConnectSocketAsync();
            if (boundSocket == socket)
                return;

            UnbindSocket();
            boundSocket = socket;
            boundSocket.ReceivedNotification += OnReceivedNotification;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[LobbyInviteService] Failed to bind invite notifications: " + exception.Message);
        }
        finally
        {
            binding = false;
        }
    }

    void UnbindSocket()
    {
        if (boundSocket != null)
            boundSocket.ReceivedNotification -= OnReceivedNotification;

        boundSocket = null;
    }

    void OnReceivedNotification(IApiNotification notification)
    {
        if (notification == null || notification.Code != LobbyInviteNotificationCode)
            return;

        LobbyInviteNotification invite;

        try
        {
            invite = JsonUtility.FromJson<LobbyInviteNotification>(notification.Content);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[LobbyInviteService] Invalid lobby invite notification: " + exception.Message);
            return;
        }

        if (invite == null || !string.Equals(invite.type, "lobby_invite", StringComparison.Ordinal))
            return;

        LobbyInvitePopup.EnsureExists().Show(
            invite,
            accept: () => _ = AcceptInviteAsync(invite),
            decline: () => { }
        );
    }

    async Task AcceptInviteAsync(LobbyInviteNotification invite)
    {
        try
        {
            await NakamaLobbyService.EnsureExists().JoinRoomByCodeAsync(new JoinRoomByCodeRequest
            {
                roomCode = invite.roomId,
                password = invite.roomId
            });

            if (!string.Equals(SceneManager.GetActiveScene().name, LobbySceneName, StringComparison.Ordinal))
                SceneManager.LoadScene(LobbySceneName);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[LobbyInviteService] Failed to accept lobby invite: " + exception.Message);
        }
    }
}
