using System;

/// <summary>
/// DTO + ghi chú contract REST/Nakama cho LobbyManager.
/// Thay mock bằng HTTP/RPC thật — giữ nguyên shape request/response.
/// </summary>
public static class LobbyApiContracts
{
    public const int DefaultMapId = 2;
    public const int MaxPageSize = 20;
}

#region Rooms

/// <summary>
/// GET /v1/lobbies?page={page}&amp;pageSize={pageSize}&amp;region={region}
/// Response 200: ListRoomsResponse
/// </summary>
[Serializable]
public class ListRoomsRequest
{
    public int page;
    public int pageSize = 5;
    public string region;
}

[Serializable]
public class ListRoomsResponse
{
    public LobbyRoomDto[] rooms;
    public int totalCount;
    public int page;
    public int pageSize;
}

[Serializable]
public class LobbyRoomDto
{
    public string roomId;
    public string roomName;
    public int mapId;
    public string mapName;
    public int currentPlayers;
    public int maxPlayers;
    public int pingMs;
    public string region;
    public bool isPrivate;
    public string hostPlayerId;
    public string matchId;
    public string status;
    public string allocationId;
    public string serverStatus;
}

/// <summary>
/// POST /v1/lobbies
/// Body: CreateRoomRequest → 201 CreateRoomResponse
/// </summary>
[Serializable]
public class CreateRoomRequest
{
    public string roomName;
    public int mapId;
    public string mapName;
    public int maxPlayers;
    public string preferredRoomId;
    public string username;
    public string displayName;
}

[Serializable]
public class CreateRoomResponse
{
    public bool success;
    public string errorMessage;
    public LobbyRoomDto room;
}

/// <summary>
/// POST /v1/lobbies/{roomId}/join
/// Body: JoinRoomRequest → 200 JoinRoomResponse
/// </summary>
[Serializable]
public class JoinRoomRequest
{
    public string roomId;
    public string matchId;
    public string password;
    public string username;
    public string displayName;
}

[Serializable]
public class JoinRoomResponse
{
    public bool success;
    public string errorMessage;
    public LobbyRoomDto room;
}

/// <summary>
/// POST /v1/lobbies/join-by-code
/// Body: { "roomCode": "AB12" } → JoinRoomResponse
/// </summary>
[Serializable]
public class JoinRoomByCodeRequest
{
    public string roomCode;
    public string password;
    public string username;
    public string displayName;
}

#endregion

#region Friends

/// <summary>
/// GET /v1/friends → FriendDto[]
/// </summary>
[Serializable]
public class FriendDto
{
    public string friendId;
    public string displayName;
    public string username;
    public bool online;
    public bool isSteamFriend;
    public string steamId;
    public string currentRoomId;
}

/// <summary>
/// GET /v1/friends/requests → FriendRequestDto[]
/// </summary>
[Serializable]
public class FriendRequestDto
{
    public string friendId;
    public string displayName;
    public string username;
    public bool isSteamFriend;
    public string steamId;
}

/// <summary>
/// POST /v1/friends/requests  Body: { "friendId": "..." }
/// </summary>
[Serializable]
public class AddFriendRequest
{
    public string friendId;
}

[Serializable]
public class FriendActionResponse
{
    public bool success;
    public string errorMessage;
}

/// <summary>
/// POST /v1/friends/{friendId}/invite  Body: { "roomId": "..." }
/// </summary>
[Serializable]
public class InviteFriendRequest
{
    public string friendId;
    public string roomId;
    public string matchId;
    public string roomName;
    public string mapName;
}

[Serializable]
public class InviteFriendResponse
{
    public bool success;
    public string errorMessage;
}

[Serializable]
public class LobbyInviteNotification
{
    public string type;
    public string senderId;
    public string senderName;
    public string roomId;
    public string matchId;
    public string roomName;
    public string mapName;
}

#endregion

#region Maps

/// <summary>
/// Một map trong dropdown Create Room. Map id = thứ tự trong list + 1.
/// </summary>
[Serializable]
public class LobbyMapOption
{
    public string displayName;
}

#endregion

#region Match

/// <summary>
/// POST /v1/lobbies/{roomId}/start  (host only)
/// Response: { "success": true, "matchId": "..." }
/// </summary>
[Serializable]
public class StartMatchRequest
{
    public string roomId;
    public string matchId;
}

[Serializable]
public class StartMatchResponse
{
    public bool success;
    public string errorMessage;
    public string matchId;
    public string status;
    public string allocationId;
    public string serverStatus;
}

#endregion
