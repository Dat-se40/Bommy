using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Middle layer lobby — UI chỉ gọi đây, không gọi REST/Nakama trực tiếp.
/// Hiện tại mock; thay body method bằng Nakama RPC / HTTP khi backend sẵn sàng.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    const float MatchServerStatusPollIntervalSeconds = 0.5f;

    public static LobbyManager Instance { get; private set; }

    public event Action<ListRoomsResponse> RoomsListed;
    public event Action<LobbyRoomDto> CurrentRoomChanged;
    public event Action<string> OperationFailed;
    public event Action<FriendDto[]> FriendsListed;
    public event Action<FriendRequestDto[]> FriendRequestsListed;

    LobbyRoomDto currentRoom;
    FriendDto[] friendsCache = Array.Empty<FriendDto>();
    CancellationTokenSource matchServerConnectCts;
    bool isConnectingToMatchServer;
    bool isCreatingRoom;
    string connectingAllocationId;
    NakamaLobbyService LobbyService => NakamaLobbyService.EnsureExists();
    FriendsService FriendService => FriendsService.EnsureExists();

    public LobbyRoomDto CurrentRoom => currentRoom;
    public bool HasCurrentRoom => currentRoom != null && !string.IsNullOrEmpty(currentRoom.roomId);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LobbyInviteService.EnsureExists();
        LobbyService.CurrentRoomUpdated -= OnServiceCurrentRoomUpdated;
        LobbyService.CurrentRoomUpdated += OnServiceCurrentRoomUpdated;
    }

    void Start()
    {
        if (SteamService.Instance != null)
        {
            SteamService.Instance.OnSteamJoinRequested -= OnSteamJoinRequested;
            SteamService.Instance.OnSteamJoinRequested += OnSteamJoinRequested;
        }
    }

    void OnDestroy()
    {
        if (!isConnectingToMatchServer)
        {
            matchServerConnectCts?.Cancel();
            matchServerConnectCts?.Dispose();
            matchServerConnectCts = null;
        }

        if (Instance == this)
            Instance = null;

        if (NakamaLobbyService.Instance != null)
            NakamaLobbyService.Instance.CurrentRoomUpdated -= OnServiceCurrentRoomUpdated;

        if (SteamService.Instance != null)
        {
            SteamService.Instance.OnSteamJoinRequested -= OnSteamJoinRequested;
        }
    }

    private void OnSteamJoinRequested(string connectString)
    {
        if (!string.IsNullOrEmpty(connectString) && connectString.StartsWith("nakama_lobby:"))
        {
            string code = connectString.Substring("nakama_lobby:".Length);
            FlowGuard.Info(FlowGuard.TagSetup, $"Steam Join requested with code: {code}");
            RequestJoinRoomByCode(new JoinRoomByCodeRequest { roomCode = code, password = code });
        }
    }

    public static LobbyManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject(nameof(LobbyManager));
        return go.AddComponent<LobbyManager>();
    }

    #region Rooms — API: ListRooms

    public async void RequestRoomList(ListRoomsRequest request = null)
    {
        try
        {
            ListRoomsResponse response = await LobbyService.ListRoomsAsync(request);

            if (response.rooms == null)
                response.rooms = Array.Empty<LobbyRoomDto>();

            RoomsListed?.Invoke(response);
        }
        catch (Exception exception)
        {
            Fail(exception.Message);
        }
    }

    #endregion

    public async void RequestCurrentRoomRefresh()
    {
        if (!LobbyService.HasCurrentRoom)
            return;

        try
        {
            await LobbyService.RefreshCurrentRoomAsync();
        }
        catch (Exception exception)
        {
            Fail(exception.Message);
        }
    }

    #region Rooms — API: CreateRoom

    public async void RequestCreateRoom(CreateRoomRequest request)
    {
        if (request == null)
        {
            Fail("CreateRoom request is null.");
            return;
        }

        if (isCreatingRoom)
            return;

        try
        {
            isCreatingRoom = true;
            LobbyRoomDto room = await LobbyService.CreateRoomAsync(request);
            FlowGuard.Info(FlowGuard.TagSetup, $"CreateRoom id={room.roomId} match={room.matchId}");
        }
        catch (Exception exception)
        {
            Fail(exception.Message);
        }
        finally
        {
            isCreatingRoom = false;
        }
    }

    #endregion

    #region Rooms — API: JoinRoom / JoinByCode

    public async void RequestJoinRoom(JoinRoomRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.roomId))
        {
            Fail("Room ID is empty.");
            return;
        }

        try
        {
            LobbyRoomDto room = await LobbyService.JoinRoomAsync(request);
            FlowGuard.Info(FlowGuard.TagSetup, $"JoinRoom id={room.roomId} match={room.matchId}");
        }
        catch (Exception exception)
        {
            Fail(exception.Message);
        }
    }

    public async void RequestJoinRoomByCode(JoinRoomByCodeRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.roomCode))
        {
            Fail("Room code is empty.");
            return;
        }

        try
        {
            LobbyRoomDto room = await LobbyService.JoinRoomByCodeAsync(request);
            FlowGuard.Info(FlowGuard.TagSetup, $"JoinRoomByCode id={room.roomId} match={room.matchId}");
        }
        catch (Exception exception)
        {
            Fail(exception.Message);
        }
    }

    #endregion

    #region Match — API: StartMatch

    public bool TryStartMatch(out string error)
    {
        error = null;

        if (!HasCurrentRoom)
        {
            error = "Create or join a room first.";
            return false;
        }

        if (IsLobbyStarting(currentRoom))
        {
            error = "Match is already starting.";
            return false;
        }

        _ = StartMatchAsync();
        return true;
    }

    async Task StartMatchAsync()
    {
        try
        {
            StartMatchResponse response = await LobbyService.StartMatchAsync(new StartMatchRequest
            {
                roomId = currentRoom.roomId,
                matchId = currentRoom.matchId
            });

            currentRoom.status = response.status;
            currentRoom.matchId = string.IsNullOrWhiteSpace(response.matchId) ? currentRoom.matchId : response.matchId;
            currentRoom.allocationId = response.allocationId;
            currentRoom.serverStatus = response.serverStatus;
            FlowGuard.Info(FlowGuard.TagSetup, $"StartMatch room={currentRoom.roomId} match={response.matchId} status={response.status}");

            CurrentRoomChanged?.Invoke(currentRoom);
            TryBeginMatchServerConnect(currentRoom);
        }
        catch (Exception exception)
        {
            Fail(exception.Message);
        }
    }

    #endregion

    #region Friends — API

    public async void RequestFriendsList()
    {
        try
        {
            friendsCache = await FriendService.ListFriendsAsync();
            FriendsListed?.Invoke(friendsCache);
        }
        catch (Exception exception)
        {
            Fail(exception.Message);
        }
    }

    public async void RequestFriendRequests()
    {
        try
        {
            FriendRequestDto[] requests = await FriendService.ListFriendRequestsAsync();
            FriendRequestsListed?.Invoke(requests);
        }
        catch (Exception exception)
        {
            Fail(exception.Message);
        }
    }

    public async void RequestAddFriend(AddFriendRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.friendId))
        {
            Fail("Friend ID is empty.");
            return;
        }

        try
        {
            await FriendService.AddFriendAsync(request.friendId);
            RequestFriendsList();
            RequestFriendRequests();
        }
        catch (Exception exception)
        {
            Fail(exception.Message);
        }
    }

    public async void RequestAcceptFriend(string friendId)
    {
        if (string.IsNullOrWhiteSpace(friendId))
        {
            Fail("Friend ID is empty.");
            return;
        }

        try
        {
            await FriendService.AcceptFriendAsync(friendId);
            RequestFriendsList();
            RequestFriendRequests();
        }
        catch (Exception exception)
        {
            Fail(exception.Message);
        }
    }

    public async void RequestDeclineFriend(string friendId)
    {
        if (string.IsNullOrWhiteSpace(friendId))
        {
            Fail("Friend ID is empty.");
            return;
        }

        try
        {
            await FriendService.DeclineFriendAsync(friendId);
            RequestFriendRequests();
        }
        catch (Exception exception)
        {
            Fail(exception.Message);
        }
    }

    public async void RequestInviteFriend(InviteFriendRequest request)
    {
        if (!HasCurrentRoom)
        {
            Fail("Create or join a room first.");
            return;
        }

        if (request == null || string.IsNullOrWhiteSpace(request.friendId))
        {
            Fail("Friend not found.");
            return;
        }

        try
        {
            request.roomId = currentRoom.roomId;
            request.matchId = currentRoom.matchId;
            request.roomName = currentRoom.roomName;
            request.mapName = currentRoom.mapName;
            await FriendService.InviteFriendToLobbyAsync(request);
            FlowGuard.Info(FlowGuard.TagSetup, $"InviteFriend friend={request.friendId} room={currentRoom.roomId}");
        }
        catch (Exception exception)
        {
            Fail(exception.Message);
        }
    }

    public void RequestJoinFriendRoom(string friendId)
    {
        FriendDto friend = FindFriend(friendId);

        if (friend == null)
        {
            Fail("Friend not found.");
            return;
        }

        if (!friend.online)
        {
            Fail(friend.displayName + " is offline.");
            return;
        }

        if (string.IsNullOrEmpty(friend.currentRoomId))
        {
            Fail(friend.displayName + " is not in a room.");
            return;
        }

        RequestJoinRoomByCode(new JoinRoomByCodeRequest
        {
            roomCode = friend.currentRoomId,
            password = friend.currentRoomId
        });
    }

    #endregion

    #region Session sync

    void ApplyCurrentRoom(LobbyRoomDto room, bool isHost)
    {
        currentRoom = room;
        GameSession.SetRoom(room.roomId, room.roomName, room.mapId, room.maxPlayers, room.mapName);
    }

    void OnServiceCurrentRoomUpdated(LobbyRoomDto room)
    {
        if (room == null)
        {
            currentRoom = null;
            CurrentRoomChanged?.Invoke(null);
            if (SteamService.Instance != null)
            {
                SteamService.Instance.ClearRichPresenceConnectString();
            }
            return;
        }

        ApplyCurrentRoom(room, room.hostPlayerId == AuthService.GetOrCreate().Session?.UserId);
        CurrentRoomChanged?.Invoke(room);
        RequestRoomList();
        if (SteamService.Instance != null)
        {
            SteamService.Instance.SetRichPresenceConnectString(room.roomId);
        }
        TryBeginMatchServerConnect(room);
    }

    public void ReplayCurrentRoom()
    {
        LobbyService.ReplayCurrentRoom();
    }

    public void ClearCurrentRoom()
    {
        if (IsLobbyStarting(currentRoom) || isConnectingToMatchServer)
        {
            Fail("Match is starting.");
            return;
        }

        currentRoom = null;
        _ = LobbyService.LeaveCurrentRoomAsync();
        CurrentRoomChanged?.Invoke(null);
        RequestRoomList();
    }

    void TryBeginMatchServerConnect(LobbyRoomDto room)
    {
        if (room == null || !IsLobbyStarting(room))
            return;

        if (string.IsNullOrWhiteSpace(room.matchId) || string.IsNullOrWhiteSpace(room.allocationId))
            return;

        if (isConnectingToMatchServer)
        {
            if (string.Equals(connectingAllocationId, room.allocationId, StringComparison.Ordinal))
                return;

            FlowGuard.Info(
                FlowGuard.TagSetup,
                $"Ignoring lobby server allocation {room.allocationId}; already connecting to {connectingAllocationId}"
            );
            return;
        }

        matchServerConnectCts?.Cancel();
        matchServerConnectCts?.Dispose();
        matchServerConnectCts = new CancellationTokenSource();
        isConnectingToMatchServer = true;
        connectingAllocationId = room.allocationId;

        FlowGuard.Info(
            FlowGuard.TagSetup,
            $"Lobby match server connect begin room={room.roomId} match={room.matchId} allocation={room.allocationId} status={room.serverStatus}"
        );

        _ = WaitForLobbyMatchServerAndConnectAsync(room.matchId, room.allocationId, matchServerConnectCts.Token);
    }

    async Task WaitForLobbyMatchServerAndConnectAsync(string matchId, string allocationId, CancellationToken cancellationToken)
    {
        try
        {
            MatchServerStatus serverStatus = await MatchServerService.EnsureExists().WaitForReadyAsync(
                matchId,
                allocationId,
                cancellationToken,
                MatchServerStatusPollIntervalSeconds
            );
            await MatchConnectionService.EnsureExists().ConnectAsync(serverStatus, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (this != null)
            {
                isConnectingToMatchServer = false;
                connectingAllocationId = null;
                Fail(exception.Message);
            }
        }
    }

    static bool IsLobbyStarting(LobbyRoomDto room)
    {
        return room != null && string.Equals(room.status, "Starting", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    public static string GenerateRoomCodePreview()
    {
        return GenerateRoomCode();
    }

    static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        char[] id = new char[4];

        for (int i = 0; i < id.Length; i++)
            id[i] = chars[UnityEngine.Random.Range(0, chars.Length)];

        return new string(id);
    }

    FriendDto FindFriend(string friendId)
    {
        for (int i = 0; i < friendsCache.Length; i++)
        {
            if (friendsCache[i].friendId == friendId)
                return friendsCache[i];
        }

        return null;
    }

    void Fail(string message)
    {
        FlowGuard.Error(FlowGuard.TagSetup, message);
        OperationFailed?.Invoke(message);
    }
}
