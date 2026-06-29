using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

public sealed class FriendsService : MonoBehaviour
{
    const string SingletonName = "[FriendsService]";
    const int FriendStateMutual = 0;
    const int FriendStateInviteSent = 1;
    const int FriendStateInviteReceived = 2;
    const int FriendListLimit = 100;

    static FriendsService instance;

    public static FriendsService Instance => instance;

    public static FriendsService EnsureExists()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<FriendsService>();

        if (instance != null)
            return instance;

        GameObject host = new(SingletonName);
        instance = host.AddComponent<FriendsService>();
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

    public async Task<FriendDto[]> ListFriendsAsync()
    {
        AuthService auth = AuthService.GetOrCreate();
        ISession session = await auth.RequireFreshSessionAsync();
        IApiFriendList response = await auth.Client.ListFriendsAsync(session, FriendStateMutual, FriendListLimit);

        List<FriendDto> friends = new();
        HashSet<string> existingSteamIds = new();

        foreach (IApiFriend friend in response.Friends)
        {
            FriendDto dto = ToFriendDto(friend);
            friends.Add(dto);
            if (!string.IsNullOrWhiteSpace(dto.steamId))
            {
                existingSteamIds.Add(dto.steamId.Trim());
            }
        }

#if !DISABLE_STEAM
        try
        {
            if (SteamService.Instance != null && SteamService.Instance.IsInitialized)
            {
                int friendCount = Steamworks.SteamFriends.GetFriendCount(Steamworks.EFriendFlags.k_EFriendFlagImmediate);
                for (int i = 0; i < friendCount; i++)
                {
                    Steamworks.CSteamID steamId = Steamworks.SteamFriends.GetFriendByIndex(i, Steamworks.EFriendFlags.k_EFriendFlagImmediate);
                    string steamIdStr = steamId.m_SteamID.ToString();

                    if (existingSteamIds.Contains(steamIdStr))
                        continue;

                    string name = Steamworks.SteamFriends.GetFriendPersonaName(steamId);
                    Steamworks.EPersonaState state = Steamworks.SteamFriends.GetFriendPersonaState(steamId);
                    bool isOnline = state != Steamworks.EPersonaState.k_EPersonaStateOffline;

                    friends.Add(new FriendDto
                    {
                        friendId = "steam:" + steamIdStr,
                        displayName = name,
                        username = string.Empty,
                        online = isOnline,
                        isSteamFriend = true,
                        steamId = steamIdStr,
                        currentRoomId = string.Empty
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FriendsService] Failed to query Steam friends: {ex.Message}");
        }
#endif

        return friends.ToArray();
    }

    public async Task<FriendRequestDto[]> ListFriendRequestsAsync()
    {
        AuthService auth = AuthService.GetOrCreate();
        ISession session = await auth.RequireFreshSessionAsync();
        IApiFriendList response = await auth.Client.ListFriendsAsync(session, FriendStateInviteReceived, FriendListLimit);

        List<FriendRequestDto> requests = new();
        foreach (IApiFriend friend in response.Friends)
            requests.Add(ToFriendRequestDto(friend));

        return requests.ToArray();
    }

    public async Task AddFriendAsync(string friendIdOrUsername)
    {
        await AddFriendInternalAsync(friendIdOrUsername);
    }

    public async Task AcceptFriendAsync(string friendId)
    {
        await AddFriendInternalAsync(friendId);
    }

    public async Task DeclineFriendAsync(string friendId)
    {
        AuthService auth = AuthService.GetOrCreate();
        ISession session = await auth.RequireFreshSessionAsync();
        await auth.Client.DeleteFriendsAsync(session, new[] { friendId });
    }

    public async Task RemoveFriendAsync(string friendId)
    {
        AuthService auth = AuthService.GetOrCreate();
        ISession session = await auth.RequireFreshSessionAsync();
        await auth.Client.DeleteFriendsAsync(session, new[] { friendId });
    }

    public async Task InviteFriendToLobbyAsync(InviteFriendRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.friendId))
            throw new InvalidOperationException("Friend not found.");

        InviteFriendResponse response = await CallInviteRpcAsync(request);

        if (response == null || !response.success)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(response?.errorMessage)
                ? "Invite failed."
                : response.errorMessage);
    }

    async Task AddFriendInternalAsync(string friendIdOrUsername)
    {
        if (string.IsNullOrWhiteSpace(friendIdOrUsername))
            throw new InvalidOperationException("Friend ID is empty.");

        string value = friendIdOrUsername.Trim();
        AuthService auth = AuthService.GetOrCreate();
        ISession session = await auth.RequireFreshSessionAsync();

        if (Guid.TryParse(value, out _))
            await auth.Client.AddFriendsAsync(session, new[] { value });
        else
            await auth.Client.AddFriendsAsync(session, null, new[] { value });
    }

    static FriendDto ToFriendDto(IApiFriend friend)
    {
        IApiUser user = friend?.User;
        return new FriendDto
        {
            friendId = user?.Id ?? string.Empty,
            displayName = DisplayName(user),
            username = user?.Username ?? string.Empty,
            online = user?.Online ?? false,
            isSteamFriend = !string.IsNullOrWhiteSpace(user?.SteamId),
            steamId = user?.SteamId ?? string.Empty,
            currentRoomId = CurrentRoomFromMetadata(user?.Metadata)
        };
    }

    static FriendRequestDto ToFriendRequestDto(IApiFriend friend)
    {
        IApiUser user = friend?.User;
        return new FriendRequestDto
        {
            friendId = user?.Id ?? string.Empty,
            displayName = DisplayName(user),
            username = user?.Username ?? string.Empty,
            isSteamFriend = !string.IsNullOrWhiteSpace(user?.SteamId),
            steamId = user?.SteamId ?? string.Empty
        };
    }

    static string DisplayName(IApiUser user)
    {
        if (!string.IsNullOrWhiteSpace(user?.DisplayName))
            return user.DisplayName;

        if (!string.IsNullOrWhiteSpace(user?.Username))
            return user.Username;

        return user?.Id ?? "Friend";
    }

    static string CurrentRoomFromMetadata(string metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return string.Empty;

        try
        {
            FriendUserMetadata parsed = JsonUtility.FromJson<FriendUserMetadata>(metadata);
            return parsed?.currentRoomId ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    static async Task<InviteFriendResponse> CallInviteRpcAsync(InviteFriendRequest request)
    {
        Task<IApiRpc> rpcTask = AuthService.GetOrCreate().RpcAsync("invite_lobby_friend", JsonUtility.ToJson(request));
        Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(12));

        Task completedTask = await Task.WhenAny(rpcTask, timeoutTask);
        if (completedTask != rpcTask)
            throw new TimeoutException("Invite server did not respond. Check backend and try again.");

        IApiRpc response = await rpcTask;
        return JsonUtility.FromJson<InviteFriendResponse>(response.Payload);
    }

    [Serializable]
    class FriendUserMetadata
    {
        public string currentRoomId = string.Empty;
    }
}
