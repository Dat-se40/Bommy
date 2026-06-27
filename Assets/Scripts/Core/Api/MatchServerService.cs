using System;
using System.Threading;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

[Serializable]
public class MatchServerRequest
{
    public string matchId;
    public string allocationId;
    public string source;
    public string roomId;
    public int mapId = 1;
    public string mapName = "Classic Garden";
    public string region = "Local";
    public int maxPlayers = 4;
}

[Serializable]
public class MatchServerAllocation
{
    public bool success;
    public string errorMessage;
    public string allocationId;
    public string matchId;
    public string source;
    public string provider;
    public string status;
}

[Serializable]
public class MatchServerStatus
{
    public bool success;
    public string errorMessage;
    public string allocationId;
    public string matchId;
    public string status;
    public string host;
    public int port;
    public string protocol;
    public string connectionToken;

    public bool IsReady => string.Equals(status, "Ready", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(host)
        && port > 0;
}

public sealed class MatchServerService : MonoBehaviour
{
    const string SingletonName = "[MatchServerService]";
    const int RpcTimeoutSeconds = 12;

    static MatchServerService instance;

    public static MatchServerService Instance => instance;
    public MatchServerAllocation CurrentAllocation { get; private set; }
    public MatchServerStatus CurrentStatus { get; private set; }

    public static MatchServerService EnsureExists()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<MatchServerService>();

        if (instance != null)
            return instance;

        GameObject host = new(SingletonName);
        instance = host.AddComponent<MatchServerService>();
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

    public async Task<MatchServerAllocation> RequestForLobbyAsync(LobbyRoomDto room)
    {
        if (room == null)
            throw new ArgumentNullException(nameof(room));

        return await RequestAllocationAsync(new MatchServerRequest
        {
            source = "CustomLobby",
            matchId = room.matchId,
            roomId = room.roomId,
            mapId = room.mapId,
            mapName = room.mapName,
            region = room.region,
            maxPlayers = room.maxPlayers
        });
    }

    public async Task<MatchServerAllocation> RequestAllocationAsync(MatchServerRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.matchId))
            throw new InvalidOperationException("Match id is required before requesting a server.");

        CurrentAllocation = await CallRpcAsync<MatchServerAllocation>(
            "request_match_server",
            JsonUtility.ToJson(request)
        );
        EnsureSuccess(CurrentAllocation?.success ?? false, CurrentAllocation?.errorMessage, "Server allocation failed.");
        return CurrentAllocation;
    }

    public async Task<MatchServerStatus> PollStatusAsync(string matchId, string allocationId = null)
    {
        CurrentStatus = await CallRpcAsync<MatchServerStatus>(
            "get_match_server_status",
            JsonUtility.ToJson(new MatchServerRequest
            {
                matchId = matchId,
                allocationId = allocationId
            })
        );
        EnsureSuccess(CurrentStatus?.success ?? false, CurrentStatus?.errorMessage, "Server status failed.");
        return CurrentStatus;
    }

    public async Task<MatchServerStatus> WaitForReadyAsync(
        string matchId,
        string allocationId,
        CancellationToken cancellationToken,
        float pollIntervalSeconds = 2f)
    {
        float startTime = Time.realtimeSinceStartup;
        string lastStatus = null;
        int polls = 0;
        Debug.LogFormat(
            "[MatchServerService] Waiting for server ready matchId={0} allocationId={1} pollInterval={2:0.000}s",
            matchId,
            allocationId,
            pollIntervalSeconds
        );

        while (!cancellationToken.IsCancellationRequested)
        {
            MatchServerStatus status = await PollStatusAsync(matchId, allocationId);
            polls++;

            if (!string.Equals(lastStatus, status.status, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogFormat(
                    "[MatchServerService] Server status={0} matchId={1} allocationId={2} elapsed={3:0.000}s polls={4}",
                    status.status,
                    status.matchId,
                    status.allocationId,
                    Time.realtimeSinceStartup - startTime,
                    polls
                );
                lastStatus = status.status;
            }

            if (status.IsReady)
            {
                Debug.LogFormat(
                    "[MatchServerService] Server ready after {0:0.000}s polls={1} endpoint={2}:{3}/{4}",
                    Time.realtimeSinceStartup - startTime,
                    polls,
                    status.host,
                    status.port,
                    status.protocol
                );
                return status;
            }

            if (string.Equals(status.status, "Failed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(status.errorMessage)
                    ? "Dedicated server allocation failed."
                    : status.errorMessage);

            int delayMs = Mathf.Max(250, Mathf.RoundToInt(pollIntervalSeconds * 1000f));
            await Task.Delay(delayMs, cancellationToken);
        }

        throw new OperationCanceledException(cancellationToken);
    }

    static async Task<T> CallRpcAsync<T>(string rpcId, string payload)
    {
        Task<IApiRpc> rpcTask = AuthService.GetOrCreate().RpcAsync(rpcId, payload);
        Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(RpcTimeoutSeconds));

        Task completedTask = await Task.WhenAny(rpcTask, timeoutTask);
        if (completedTask != rpcTask)
            throw new TimeoutException("Match server did not respond. Check backend and try again.");

        IApiRpc response = await rpcTask;
        T result = JsonUtility.FromJson<T>(response.Payload);

        if (result == null)
            throw new InvalidOperationException("Nakama returned invalid match server data.");

        return result;
    }

    static void EnsureSuccess(bool success, string errorMessage, string fallback)
    {
        if (success)
            return;

        throw new InvalidOperationException(string.IsNullOrWhiteSpace(errorMessage) ? fallback : errorMessage);
    }
}
