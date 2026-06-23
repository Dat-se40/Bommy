using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Middle layer lobby — UI chỉ gọi đây, không gọi REST/Nakama trực tiếp.
/// Hiện tại mock; thay body method bằng Nakama RPC / HTTP khi backend sẵn sàng.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public event Action<ListRoomsResponse> RoomsListed;
    public event Action<LobbyRoomDto> CurrentRoomChanged;
    public event Action<string> OperationFailed;
    public event Action<FriendDto[]> FriendsListed;
    public event Action<FriendRequestDto[]> FriendRequestsListed;

    readonly List<LobbyRoomDto> mockRooms = new();
    readonly List<FriendDto> mockFriends = new();
    readonly List<FriendRequestDto> mockFriendRequests = new();

    LobbyRoomDto currentRoom;

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
        SeedMockData();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static LobbyManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject(nameof(LobbyManager));
        return go.AddComponent<LobbyManager>();
    }

    #region Rooms — API: ListRooms

    public void RequestRoomList(ListRoomsRequest request = null)
    {
        request ??= new ListRoomsRequest { page = 0, pageSize = LobbyApiContracts.MaxPageSize };

        int page = Mathf.Max(0, request.page);
        int pageSize = Mathf.Clamp(request.pageSize, 1, LobbyApiContracts.MaxPageSize);
        int start = page * pageSize;
        int end = Mathf.Min(start + pageSize, mockRooms.Count);

        var slice = new LobbyRoomDto[Mathf.Max(0, end - start)];
        for (int i = start; i < end; i++)
            slice[i - start] = mockRooms[i];

        var response = new ListRoomsResponse
        {
            rooms = slice,
            totalCount = mockRooms.Count,
            page = page,
            pageSize = pageSize
        };

        FlowGuard.Info(FlowGuard.TagSetup, $"ListRooms mock page={page} count={slice.Length}");
        RoomsListed?.Invoke(response);
    }

    #endregion

    #region Rooms — API: CreateRoom

    public void RequestCreateRoom(CreateRoomRequest request)
    {
        if (request == null)
        {
            Fail("CreateRoom request is null.");
            return;
        }

        string roomName = string.IsNullOrWhiteSpace(request.roomName)
            ? "Casual Room"
            : request.roomName.Trim();

        string roomId = string.IsNullOrWhiteSpace(request.preferredRoomId)
            ? GenerateRoomCode()
            : request.preferredRoomId.Trim().ToUpperInvariant();

        bool isPrivate = !string.IsNullOrEmpty(request.password);

        var room = new LobbyRoomDto
        {
            roomId = roomId,
            roomName = roomName,
            mapId = request.mapId > 0 ? request.mapId : LobbyApiContracts.DefaultMapId,
            mapName = string.IsNullOrEmpty(request.mapName) ? "Classic Garden" : request.mapName,
            currentPlayers = 1,
            maxPlayers = Mathf.Clamp(request.maxPlayers, 2, 4),
            pingMs = 0,
            region = "Local",
            isPrivate = isPrivate,
            hostPlayerId = LocalPlayerId()
        };

        mockRooms.Insert(0, room);
        ApplyCurrentRoom(room, isHost: true);

        if (isPrivate)
        {
            PlayerPrefs.SetString("CurrentRoomPassword", request.password);
            PlayerPrefs.SetInt("CurrentRoomIsPrivate", 1);
        }
        else
        {
            PlayerPrefs.SetString("CurrentRoomPassword", "");
            PlayerPrefs.SetInt("CurrentRoomIsPrivate", 0);
        }

        PlayerPrefs.Save();

        FlowGuard.Info(FlowGuard.TagSetup, $"CreateRoom mock id={roomId} name={roomName}");
        CurrentRoomChanged?.Invoke(room);
    }

    #endregion

    #region Rooms — API: JoinRoom / JoinByCode

    public void RequestJoinRoom(JoinRoomRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.roomId))
        {
            Fail("Room ID is empty.");
            return;
        }

        LobbyRoomDto room = FindRoom(request.roomId);

        if (room == null)
        {
            Fail("Room not found: " + request.roomId);
            return;
        }

        if (room.isPrivate && !ValidatePassword(room, request.password))
        {
            Fail("Incorrect password.");
            return;
        }

        CompleteJoin(room);
    }

    public void RequestJoinRoomByCode(JoinRoomByCodeRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.roomCode))
        {
            Fail("Room code is empty.");
            return;
        }

        RequestJoinRoom(new JoinRoomRequest
        {
            roomId = request.roomCode.Trim().ToUpperInvariant(),
            password = request.password
        });
    }

    void CompleteJoin(LobbyRoomDto room)
    {
        room.currentPlayers = Mathf.Min(room.currentPlayers + 1, room.maxPlayers);
        ApplyCurrentRoom(room, isHost: false);
        FlowGuard.Info(FlowGuard.TagSetup, $"JoinRoom mock id={room.roomId}");
        CurrentRoomChanged?.Invoke(room);
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

        // TODO[NETWORK] POST /v1/lobbies/{roomId}/start — host start PurrNet + EdgeGap.
        FlowGuard.Info(FlowGuard.TagSetup, $"StartMatch mock room={currentRoom.roomId}");
        return true;
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

        RequestJoinRoomByCode(new JoinRoomByCodeRequest { roomCode = friend.currentRoomId });
    }

    #endregion

    #region Session sync

    void ApplyCurrentRoom(LobbyRoomDto room, bool isHost)
    {
        currentRoom = room;
        GameSession.SetRoom(room.roomName, room.mapId, room.maxPlayers, room.mapName);
    }

    public void ClearCurrentRoom()
    {
        currentRoom = null;
    }

    #endregion

    #region Mock data

    void SeedMockData()
    {
        mockRooms.Clear();
        mockRooms.Add(MakeRoom("RX45", "SG Casual Play", 1, 4, 45, "Singapore", false));
        mockRooms.Add(MakeRoom("VN01", "VN PRO ONLY", 1, 4, 30, "Singapore", true));
        mockRooms.Add(MakeRoom("USW1", "US West Fun Match", 2, 4, 150, "US West", false));
        mockRooms.Add(MakeRoom("EU01", "Europe Chill Room", 1, 4, 210, "Europe", false));
        mockRooms.Add(MakeRoom("JP01", "Japan Dev Test", 4, 4, 90, "Japan", false));
        mockRooms.Add(MakeRoom("DEV1", "Secret Room (dev)", 2, 4, 52, "Singapore", true));
        mockRooms.Add(MakeRoom("M007", "Mock Lobby 7", 1, 2, 80, "US East", false));
        mockRooms.Add(MakeRoom("M008", "Mock Lobby 8", 2, 3, 110, "Australia", false));

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

    static LobbyRoomDto MakeRoom(
        string id,
        string name,
        int players,
        int max,
        int ping,
        string region,
        bool isPrivate
    )
    {
        return new LobbyRoomDto
        {
            roomId = id,
            roomName = name,
            mapId = LobbyApiContracts.DefaultMapId,
            mapName = "Classic Garden",
            currentPlayers = players,
            maxPlayers = max,
            pingMs = ping,
            region = region,
            isPrivate = isPrivate,
            hostPlayerId = "host-" + id
        };
    }

    static string LocalPlayerId() => "local-player";

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

    LobbyRoomDto FindRoom(string roomId)
    {
        string code = roomId.Trim().ToUpperInvariant();

        for (int i = 0; i < mockRooms.Count; i++)
        {
            if (mockRooms[i].roomId == code)
                return mockRooms[i];
        }

        return null;
    }

    static bool ValidatePassword(LobbyRoomDto room, string password)
    {
        if (!room.isPrivate)
            return true;

        // Mock: dev room VN01 password "123", DEV1 password "dev"
        if (room.roomId == "VN01")
            return password == "123";

        if (room.roomId == "DEV1")
            return password == "dev";

        return password == PlayerPrefs.GetString("CurrentRoomPassword", "");
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
