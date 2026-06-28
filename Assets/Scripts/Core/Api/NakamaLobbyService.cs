using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

/// <summary>
/// Client facade for Nakama-backed custom lobby RPCs and match socket membership.
/// </summary>
public sealed class NakamaLobbyService : MonoBehaviour
{
    const string SingletonName = "[NakamaLobbyService]";
    const int LobbyRpcTimeoutSeconds = 12;

    static NakamaLobbyService instance;

    IMatch currentMatch;
    ISocket boundSocket;

    public static NakamaLobbyService Instance => instance;
    public event Action<LobbyRoomDto> CurrentRoomUpdated;
    public LobbyRoomDto CurrentRoom { get; private set; }
    public bool HasCurrentRoom => CurrentRoom != null && !string.IsNullOrEmpty(CurrentRoom.roomId);

    public static NakamaLobbyService EnsureExists()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<NakamaLobbyService>();

        if (instance != null)
            return instance;

        GameObject host = new(SingletonName);
        instance = host.AddComponent<NakamaLobbyService>();
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
        if (boundSocket != null)
            boundSocket.ReceivedMatchPresence -= OnMatchPresence;

        if (instance == this)
            instance = null;
    }

    public Task<ListRoomsResponse> ListRoomsAsync(ListRoomsRequest request)
    {
        request ??= new ListRoomsRequest { page = 0, pageSize = LobbyApiContracts.MaxPageSize };
        return CallLobbyRpcAsync<ListRoomsResponse>("list_lobbies", JsonUtility.ToJson(request));
    }

    public async Task<LobbyRoomDto> CreateRoomAsync(CreateRoomRequest request)
    {
        CreateRoomResponse response = await CallLobbyRpcAsync<CreateRoomResponse>(
            "create_lobby",
            JsonUtility.ToJson(request)
        );
        EnsureSuccess(response?.success ?? false, response?.errorMessage, "Create room failed.");
        return await JoinSocketMatchAsync(response.room);
    }

    public async Task<LobbyRoomDto> JoinRoomAsync(JoinRoomRequest request)
    {
        JoinRoomResponse response = await CallLobbyRpcAsync<JoinRoomResponse>(
            "join_lobby",
            JsonUtility.ToJson(request)
        );
        EnsureSuccess(response?.success ?? false, response?.errorMessage, "Join room failed.");
        return await JoinSocketMatchAsync(response.room);
    }

    public async Task<LobbyRoomDto> JoinRoomByCodeAsync(JoinRoomByCodeRequest request)
    {
        JoinRoomResponse response = await CallLobbyRpcAsync<JoinRoomResponse>(
            "join_lobby_by_code",
            JsonUtility.ToJson(request)
        );
        EnsureSuccess(response?.success ?? false, response?.errorMessage, "Join room failed.");
        return await JoinSocketMatchAsync(response.room);
    }

    public async Task LeaveCurrentRoomAsync()
    {
        LobbyRoomDto room = CurrentRoom;
        IMatch match = currentMatch;

        CurrentRoom = null;
        currentMatch = null;

        if (room != null)
        {
            await CallLobbyRpcAsync<JoinRoomResponse>(
                "leave_lobby",
                JsonUtility.ToJson(new JoinRoomRequest
                {
                    roomId = room.roomId,
                    matchId = room.matchId
                })
            );
        }

        if (match != null && AuthService.GetOrCreate().Socket != null)
            await AuthService.GetOrCreate().Socket.LeaveMatchAsync(match);
    }

    public async Task<LobbyRoomDto> RefreshCurrentRoomAsync()
    {
        if (CurrentRoom == null || string.IsNullOrEmpty(CurrentRoom.matchId))
            return CurrentRoom;

        JoinRoomResponse response = await CallLobbyRpcAsync<JoinRoomResponse>(
            "get_lobby",
            JsonUtility.ToJson(new JoinRoomRequest
            {
                roomId = CurrentRoom.roomId,
                matchId = CurrentRoom.matchId
            })
        );

        if (response == null || !response.success || response.room == null)
        {
            CurrentRoom = null;
            currentMatch = null;
            CurrentRoomUpdated?.Invoke(null);
            return null;
        }

        CurrentRoom = response.room;
        CurrentRoomUpdated?.Invoke(CurrentRoom);
        return CurrentRoom;
    }

    public void ReplayCurrentRoom()
    {
        if (CurrentRoom != null)
            CurrentRoomUpdated?.Invoke(CurrentRoom);
    }

    public Task<StartMatchResponse> StartMatchAsync(StartMatchRequest request)
    {
        if (request == null && CurrentRoom != null)
        {
            request = new StartMatchRequest
            {
                roomId = CurrentRoom.roomId,
                matchId = CurrentRoom.matchId
            };
        }

        return StartMatchInternalAsync(request);
    }

    async Task<LobbyRoomDto> JoinSocketMatchAsync(LobbyRoomDto room)
    {
        if (room == null || string.IsNullOrEmpty(room.matchId))
            throw new InvalidOperationException("Lobby response did not include a match id.");

        ISocket socket = await AuthService.GetOrCreate().ConnectSocketAsync();
        BindSocket(socket);

        if (currentMatch != null && currentMatch.Id != room.matchId)
            await socket.LeaveMatchAsync(currentMatch);

        currentMatch = await socket.JoinMatchAsync(room.matchId, new Dictionary<string, string>());
        room.currentPlayers = Mathf.Clamp(Math.Max(1, Math.Max(room.currentPlayers, currentMatch.Size)), 1, room.maxPlayers);
        CurrentRoom = room;
        CurrentRoomUpdated?.Invoke(CurrentRoom);
        return CurrentRoom;
    }

    async Task<StartMatchResponse> StartMatchInternalAsync(StartMatchRequest request)
    {
        StartMatchResponse response = await CallLobbyRpcAsync<StartMatchResponse>(
            "start_lobby_match",
            JsonUtility.ToJson(request)
        );
        EnsureSuccess(response?.success ?? false, response?.errorMessage, "Start match failed.");

        if (CurrentRoom != null && response != null && CurrentRoom.matchId == response.matchId)
        {
            CurrentRoom.status = response.status;
            CurrentRoom.allocationId = response.allocationId;
            CurrentRoom.serverStatus = response.serverStatus;
            CurrentRoomUpdated?.Invoke(CurrentRoom);
        }

        return response;
    }

    void BindSocket(ISocket socket)
    {
        if (boundSocket == socket)
            return;

        if (boundSocket != null)
            boundSocket.ReceivedMatchPresence -= OnMatchPresence;

        boundSocket = socket;

        if (boundSocket != null)
            boundSocket.ReceivedMatchPresence += OnMatchPresence;
    }

    void OnMatchPresence(IMatchPresenceEvent presenceEvent)
    {
        if (CurrentRoom == null || presenceEvent == null || presenceEvent.MatchId != CurrentRoom.matchId)
            return;

        string selfUserId = AuthService.GetOrCreate().Session?.UserId;
        CurrentRoom.currentPlayers = Mathf.Clamp(
            CurrentRoom.currentPlayers + Count(presenceEvent.Joins, selfUserId) - Count(presenceEvent.Leaves, selfUserId),
            0,
            CurrentRoom.maxPlayers
        );
        CurrentRoomUpdated?.Invoke(CurrentRoom);
    }

    static int Count(IEnumerable<IUserPresence> presences, string excludedUserId)
    {
        if (presences == null)
            return 0;

        int count = 0;
        foreach (IUserPresence presence in presences)
        {
            if (!string.IsNullOrEmpty(excludedUserId) && presence != null && presence.UserId == excludedUserId)
                continue;

            count++;
        }

        return count;
    }

    static async Task<T> CallLobbyRpcAsync<T>(string rpcId, string payload)
    {
        Task<IApiRpc> rpcTask = AuthService.GetOrCreate().RpcAsync(rpcId, payload);
        Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(LobbyRpcTimeoutSeconds));

        Task completedTask = await Task.WhenAny(rpcTask, timeoutTask);
        if (completedTask != rpcTask)
            throw new TimeoutException("Lobby server did not respond. Check backend and try again.");

        IApiRpc response = await rpcTask;
        T result = JsonUtility.FromJson<T>(response.Payload);

        if (result == null)
            throw new InvalidOperationException("Nakama returned invalid lobby data.");

        return result;
    }

    static void EnsureSuccess(bool success, string errorMessage, string fallback)
    {
        if (success)
            return;

        throw new InvalidOperationException(string.IsNullOrWhiteSpace(errorMessage) ? fallback : errorMessage);
    }
}
