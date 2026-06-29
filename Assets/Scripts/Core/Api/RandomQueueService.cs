using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

[Serializable]
public class RandomQueueRequest
{
    public int mapId = 0;
    public string mapName = "Random";
    public string region = "Local";
    public int maxPlayers = 4;
    public string username;
    public string displayName;
    public int selectedCharacterId = 1;
}

[Serializable]
public class RandomQueueTicketRequest
{
    public string ticketId;
}

[Serializable]
public class RandomQueueStatus
{
    public bool success;
    public string errorMessage;
    public string ticketId;
    public string status;
    public string matchId;
    public int playerCount;
    public int maxPlayers;
    public int acceptedCount;
    public string allocationId;
    public string serverStatus;
    public RandomMatchDto match;
}

[Serializable]
public class RandomMatchDto
{
    public string matchId;
    public string roomId;
    public string status;
    public string allocationId;
    public string serverStatus;
    public RandomQueuePlayerDto[] players;
}

[Serializable]
public class RandomQueuePlayerDto
{
    public string userId;
    public string username;
    public string displayName;
    public int selectedCharacterId;
}

public sealed class RandomQueueService : MonoBehaviour
{
    const string SingletonName = "[RandomQueueService]";
    const int QueueRpcTimeoutSeconds = 12;
    const int QueueRpcAttempts = 3;

    static RandomQueueService instance;

    public static RandomQueueService Instance => instance;
    public RandomQueueStatus CurrentStatus { get; private set; }

    public static RandomQueueService EnsureExists()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<RandomQueueService>();

        if (instance != null)
            return instance;

        GameObject host = new(SingletonName);
        instance = host.AddComponent<RandomQueueService>();
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

    public async Task<RandomQueueStatus> JoinQueueAsync(RandomQueueRequest request)
    {
        request ??= new RandomQueueRequest();
        request.maxPlayers = 4;
        AuthService authService = AuthService.GetOrCreate();
        request.username = authService.Username;
        request.displayName = authService.DisplayName;
        request.selectedCharacterId = Mathf.Max(1, PlayerProgressionService.Instance?.Current?.selectedCharacterId ?? 1);
        CurrentStatus = await CallQueueRpcAsync("join_random_queue", JsonUtility.ToJson(request));
        EnsureSuccess(CurrentStatus, "Join queue failed.");
        return CurrentStatus;
    }

    public async Task<RandomQueueStatus> PollQueueAsync(string ticketId)
    {
        CurrentStatus = await CallQueueRpcAsync(
            "poll_random_queue",
            JsonUtility.ToJson(new RandomQueueTicketRequest { ticketId = ticketId })
        );
        EnsureSuccess(CurrentStatus, "Queue status failed.");
        return CurrentStatus;
    }

    public async Task<RandomQueueStatus> CancelQueueAsync(string ticketId)
    {
        CurrentStatus = await CallQueueRpcAsync(
            "cancel_random_queue",
            JsonUtility.ToJson(new RandomQueueTicketRequest { ticketId = ticketId })
        );
        EnsureSuccess(CurrentStatus, "Cancel queue failed.");
        return CurrentStatus;
    }

    public async Task<RandomQueueStatus> AcceptMatchAsync(string ticketId)
    {
        CurrentStatus = await CallQueueRpcAsync(
            "accept_random_match",
            JsonUtility.ToJson(new RandomQueueTicketRequest { ticketId = ticketId })
        );
        EnsureSuccess(CurrentStatus, "Accept match failed.");
        return CurrentStatus;
    }

    static async Task<RandomQueueStatus> CallQueueRpcAsync(string rpcId, string payload)
    {
        Exception lastException = null;

        for (int attempt = 1; attempt <= QueueRpcAttempts; attempt++)
        {
            try
            {
                Task<IApiRpc> rpcTask = AuthService.GetOrCreate().RpcAsync(rpcId, payload);
                Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(QueueRpcTimeoutSeconds));

                Task completedTask = await Task.WhenAny(rpcTask, timeoutTask);
                if (completedTask != rpcTask)
                    throw new TimeoutException("Queue server did not respond. Check backend and try again.");

                IApiRpc response = await rpcTask;
                RandomQueueStatus result = JsonUtility.FromJson<RandomQueueStatus>(response.Payload);

                if (result == null)
                    throw new InvalidOperationException("Nakama returned invalid queue data.");

                return result;
            }
            catch (Exception exception)
            {
                lastException = exception;

                if (!IsTransientQueueError(exception) || attempt >= QueueRpcAttempts)
                    break;

                await Task.Delay(200 * attempt);
            }
        }

        throw lastException ?? new InvalidOperationException("Queue request failed.");
    }

    static void EnsureSuccess(RandomQueueStatus status, string fallback)
    {
        if (status != null && status.success)
            return;

        string message = status != null ? status.errorMessage : null;
        throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? fallback : message);
    }

    static bool IsTransientQueueError(Exception exception)
    {
        string message = exception?.Message ?? "";
        return message.IndexOf("match busy", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
