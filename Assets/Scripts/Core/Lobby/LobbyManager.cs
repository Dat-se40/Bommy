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

    readonly List<FriendDto> mockFriends = new();
    readonly List<FriendRequestDto> mockFriendRequests = new();

    LobbyRoomDto currentRoom;
    CancellationTokenSource matchServerConnectCts;
    bool isConnectingToMatchServer;
    string connectingAllocationId;
    NakamaLobbyService LobbyService => NakamaLobbyService.EnsureExists();

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
        SeedMockFriendData();
        LobbyService.CurrentRoomUpdated -= OnServiceCurrentRoomUpdated;
        LobbyService.CurrentRoomUpdated += OnServiceCurrentRoomUpdated;
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

        try
        {
            LobbyRoomDto room = await LobbyService.CreateRoomAsync(request);
            FlowGuard.Info(FlowGuard.TagSetup, $"CreateRoom id={room.roomId} match={room.matchId}");
        }
        catch (Exception exception)
        {
            Fail(exception.Message);
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

    #region Friends — API stubs

    public void RequestFriendsList()
    {
        FriendsListed?.Invoke(mockFriends.ToArray());
    }

    public void RequestFriendRequests()
    {
        FriendRequestsListed?.Invoke(mockFriendRequests.ToArray());
    }

    public void RequestAddFriend(AddFriendRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.friendId))
        {
            Fail("Friend ID is empty.");
            return;
        }

        string id = request.friendId.Trim();

        if (FindFriend(id) != null)
        {
            Fail("Friend already exists.");
            return;
        }

        if (FindFriendRequest(id) != null)
        {
            Fail("Request already pending.");
            return;
        }

        mockFriendRequests.Insert(0, new FriendRequestDto
        {
            friendId = id,
            displayName = "Friend " + id,
            isSteamFriend = false
        });

        RequestFriendRequests();
    }

    public void RequestAcceptFriend(string friendId)
    {
        for (int i = 0; i < mockFriendRequests.Count; i++)
        {
            if (mockFriendRequests[i].friendId != friendId)
                continue;

            FriendRequestDto req = mockFriendRequests[i];
            mockFriends.Insert(0, new FriendDto
            {
                friendId = req.friendId,
                displayName = req.displayName,
                online = true,
                isSteamFriend = req.isSteamFriend,
                currentRoomId = ""
            });
            mockFriendRequests.RemoveAt(i);
            RequestFriendsList();
            RequestFriendRequests();
            return;
        }

        Fail("Request not found.");
    }

    public void RequestDeclineFriend(string friendId)
    {
        for (int i = 0; i < mockFriendRequests.Count; i++)
        {
            if (mockFriendRequests[i].friendId != friendId)
                continue;

            mockFriendRequests.RemoveAt(i);
            RequestFriendRequests();
            return;
        }

        Fail("Request not found.");
    }

    public void RequestInviteFriend(InviteFriendRequest request)
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

        // TODO[STEAM/NETWORK] POST invite endpoint.
        FlowGuard.Info(
            FlowGuard.TagSetup,
            $"InviteFriend mock friend={request.friendId} room={currentRoom.roomId}"
        );
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
            return;
        }

        ApplyCurrentRoom(room, room.hostPlayerId == AuthService.GetOrCreate().Session?.UserId);
        CurrentRoomChanged?.Invoke(room);
        RequestRoomList();
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

    #region Friend stubs

    void SeedMockFriendData()
    {
        mockFriends.Clear();
        mockFriends.Add(new FriendDto
        {
            friendId = "1001",
            displayName = "MimiFan",
            online = true,
            isSteamFriend = true,
            currentRoomId = "RX45"
        });
        mockFriends.Add(new FriendDto
        {
            friendId = "1002",
            displayName = "BomberCat",
            online = true,
            isSteamFriend = true,
            currentRoomId = ""
        });
        mockFriends.Add(new FriendDto
        {
            friendId = "2001",
            displayName = "LocalBuddy",
            online = true,
            isSteamFriend = false,
            currentRoomId = "MM88"
        });
        mockFriends.Add(new FriendDto
        {
            friendId = "3001",
            displayName = "OfflineDog",
            online = false,
            isSteamFriend = false,
            currentRoomId = ""
        });

        mockFriendRequests.Clear();
        mockFriendRequests.Add(new FriendRequestDto
        {
            friendId = "9001",
            displayName = "NewBomber",
            isSteamFriend = false
        });
        mockFriendRequests.Add(new FriendRequestDto
        {
            friendId = "STEAM42",
            displayName = "SteamGuest",
            isSteamFriend = true
        });
    }

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
        for (int i = 0; i < mockFriends.Count; i++)
        {
            if (mockFriends[i].friendId == friendId)
                return mockFriends[i];
        }

        return null;
    }

    FriendRequestDto FindFriendRequest(string friendId)
    {
        for (int i = 0; i < mockFriendRequests.Count; i++)
        {
            if (mockFriendRequests[i].friendId == friendId)
                return mockFriendRequests[i];
        }

        return null;
    }

    void Fail(string message)
    {
        FlowGuard.Error(FlowGuard.TagSetup, message);
        OperationFailed?.Invoke(message);
    }

    #endregion
}
