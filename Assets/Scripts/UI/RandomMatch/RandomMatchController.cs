using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RandomMatchController : MonoBehaviour
{
    [Serializable]
    public class PlayerSlotUI
    {
        public Image avatar;
        public TMP_Text namelbl;
        public TMP_Text statelbl;
        public Image background;
    }

    [Header("Scenes")]
    [SerializeField] private string characterSelectSceneName = "CharacterSelect";

    [Header("UI")]
    [SerializeField] private TMP_Text roomIdlbl;
    [SerializeField] private TMP_Text statuslbl;
    [SerializeField] private PlayerSlotUI[] playerSlots;
    [SerializeField] private Button cancelbtn;
    [SerializeField] private Button startbtn;

    [Header("Queue")]
    [SerializeField] private int mapId = 0;
    [SerializeField] private string mapName = "Random";
    [SerializeField] private string region = "Local";
    [SerializeField] private float pollIntervalSeconds = 2f;
    [SerializeField] private float serverStatusPollIntervalSeconds = 0.5f;

    [Header("Avatars")]
    [SerializeField] private Sprite defaultSearchingAvatar;
    [SerializeField] private Sprite selectedCharacterAvatar;

    string ticketId;
    bool polling;
    bool requestInProgress;
    bool serverFlowStarted;
    CancellationTokenSource serverFlowCts;

    async void Start()
    {
        SetupButtons();
        SetupInitialSlots();
        await JoinQueueAsync();
    }

    void OnDestroy()
    {
        polling = false;
        if (!serverFlowStarted)
        {
            serverFlowCts?.Cancel();
            serverFlowCts?.Dispose();
            serverFlowCts = null;
        }
    }

    void SetupButtons()
    {
        if (cancelbtn != null)
        {
            cancelbtn.onClick.RemoveAllListeners();
            cancelbtn.onClick.AddListener(CancelMatch);
        }

        if (startbtn != null)
        {
            startbtn.onClick.RemoveAllListeners();
            startbtn.interactable = false;
            startbtn.gameObject.SetActive(false);
        }
    }

    void SetupInitialSlots()
    {
        if (roomIdlbl != null)
            roomIdlbl.text = "Queue: -";

        SetStatus("Searching for players... 1/4");

        if (playerSlots == null)
            return;

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (i == 0)
                SetupLocalPlayerSlot(playerSlots[i]);
            else
                SetupSearchingSlot(playerSlots[i]);
        }
    }

    async Task JoinQueueAsync()
    {
        try
        {
            RandomQueueStatus status = await RandomQueueService.EnsureExists().JoinQueueAsync(new RandomQueueRequest
            {
                mapId = mapId,
                mapName = mapName,
                region = region,
                maxPlayers = 4
            });

            ticketId = status.ticketId;
            Debug.Log("[RandomMatchController] Joined random queue ticket=" + ticketId + " status=" + status.status, this);
            ApplyStatus(status);
            polling = true;
            _ = PollLoopAsync();
        }
        catch (Exception exception)
        {
            SetError(exception.Message);
        }
    }

    async Task PollLoopAsync()
    {
        Debug.Log("[RandomMatchController] Poll loop started ticket=" + ticketId + " interval=" + pollIntervalSeconds, this);

        while (polling && !string.IsNullOrEmpty(ticketId))
        {
            await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds));

            if (!polling)
                return;

            try
            {
                RandomQueueStatus status = await RandomQueueService.EnsureExists().PollQueueAsync(ticketId);
                ApplyStatus(status);

                if (status.status == "Cancelled" ||
                    status.status == "Expired")
                {
                    polling = false;
                }
            }
            catch (Exception exception)
            {
                SetError(exception.Message);
            }
        }
    }

    void ApplyStatus(RandomQueueStatus status)
    {
        if (status == null)
            return;

        if (!string.IsNullOrEmpty(status.matchId) && roomIdlbl != null)
            roomIdlbl.text = "Match: " + status.matchId;
        else if (roomIdlbl != null)
            roomIdlbl.text = "Ticket: " + status.ticketId;

        if (status.status == "MatchFound" || status.status == "Accepted")
        {
            if (status.status == "Accepted")
            {
                if (string.IsNullOrWhiteSpace(status.allocationId))
                {
                    SetStatus("Match locked. Waiting for match server...");
                }
                else
                {
                    SetStatus(string.IsNullOrWhiteSpace(status.serverStatus)
                        ? "Match locked. Waiting for match server..."
                        : FormatServerStatus(status.serverStatus));
                    BeginServerFlow(status);
                }
            }
            else
            {
                SetStatus("Match found. Locking players...");
            }

            ApplyMatchedPlayers(status.match);

            if (startbtn != null)
                startbtn.interactable = false;

            if (cancelbtn != null)
                cancelbtn.interactable = false;

            return;
        }

        if (status.status == "Cancelled")
        {
            SetStatus("Queue cancelled.");
            if (startbtn != null)
                startbtn.interactable = false;
            if (cancelbtn != null)
                cancelbtn.interactable = true;
            return;
        }

        if (status.status == "Expired")
        {
            SetStatus("Previous match finished. Queue again to find a new match.");
            if (startbtn != null)
                startbtn.interactable = false;
            if (cancelbtn != null)
                cancelbtn.interactable = true;
            return;
        }

        SetStatus("Searching for players... " + Mathf.Max(1, status.playerCount) + "/" + status.maxPlayers);
        FillSearchingSlots(Mathf.Max(1, status.playerCount));

        if (startbtn != null)
            startbtn.interactable = false;

        if (cancelbtn != null)
            cancelbtn.interactable = true;
    }

    void ApplyMatchedPlayers(RandomMatchDto match)
    {
        if (playerSlots == null)
            return;

        RandomQueuePlayerDto[] players = match?.players;
        int count = players != null ? players.Length : 0;

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (i < count)
            {
                FillPlayerSlot(playerSlots[i], players[i].displayName);
            }
            else
            {
                SetupSearchingSlot(playerSlots[i]);
            }
        }
    }

    void FillSearchingSlots(int playerCount)
    {
        if (playerSlots == null)
            return;

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (i == 0)
            {
                SetupLocalPlayerSlot(playerSlots[i]);
                continue;
            }

            if (i < playerCount)
                FillPlayerSlot(playerSlots[i], "Queued Player " + (i + 1));
            else
                SetupSearchingSlot(playerSlots[i]);
        }
    }

    void SetupLocalPlayerSlot(PlayerSlotUI slot)
    {
        FillPlayerSlot(slot, AuthService.GetOrCreate().DisplayName);
    }

    void SetupSearchingSlot(PlayerSlotUI slot)
    {
        if (slot == null)
            return;

        if (slot.avatar != null)
        {
            if (defaultSearchingAvatar != null)
            {
                slot.avatar.sprite = defaultSearchingAvatar;
                slot.avatar.enabled = true;
            }
            else
            {
                slot.avatar.enabled = false;
            }
        }

        if (slot.namelbl != null)
            slot.namelbl.text = "Searching...";

        if (slot.statelbl != null)
            slot.statelbl.text = "Waiting";

        if (slot.background != null)
            slot.background.color = HexToColor("#FFE6BC");
    }

    void FillPlayerSlot(PlayerSlotUI slot, string playerName)
    {
        if (slot == null)
            return;

        if (slot.avatar != null)
        {
            if (selectedCharacterAvatar != null)
            {
                slot.avatar.sprite = selectedCharacterAvatar;
                slot.avatar.enabled = true;
            }
            else
            {
                slot.avatar.enabled = false;
            }
        }

        if (slot.namelbl != null)
            slot.namelbl.text = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName;

        if (slot.statelbl != null)
            slot.statelbl.text = "Ready";

        if (slot.background != null)
            slot.background.color = HexToColor("#FFF1C9");
    }

    public async void CancelMatch()
    {
        if (requestInProgress)
            return;

        requestInProgress = true;

        if (cancelbtn != null)
            cancelbtn.interactable = false;

        if (!string.IsNullOrEmpty(ticketId))
        {
            try
            {
                RandomQueueStatus status = await RandomQueueService.EnsureExists().CancelQueueAsync(ticketId);
                ApplyStatus(status);

                if (status.status == "Cancelled")
                {
                    polling = false;
                    SceneManager.LoadScene(characterSelectSceneName);
                    return;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[RandomMatchController] Cancel queue failed, returning to menu: " + exception.Message, this);
                polling = false;
                SceneManager.LoadScene(characterSelectSceneName);
                return;
            }
            finally
            {
                requestInProgress = false;
            }

            return;
        }

        polling = false;
        SceneManager.LoadScene(characterSelectSceneName);
    }

    public async void AcceptMatch()
    {
        if (requestInProgress || string.IsNullOrEmpty(ticketId))
            return;

        requestInProgress = true;

        if (startbtn != null)
            startbtn.interactable = false;

        try
        {
            RandomQueueStatus status = await RandomQueueService.EnsureExists().AcceptMatchAsync(ticketId);
            ApplyStatus(status);
        }
        catch (Exception exception)
        {
            SetError(exception.Message);
        }
        finally
        {
            requestInProgress = false;
        }
    }

    void BeginServerFlow(RandomQueueStatus status)
    {
        if (serverFlowStarted || string.IsNullOrEmpty(ticketId))
            return;

        if (status == null || string.IsNullOrWhiteSpace(status.allocationId))
            return;

        ApplyGameSession(status);
        serverFlowStarted = true;
        polling = false;
        serverFlowCts?.Cancel();
        serverFlowCts?.Dispose();
        serverFlowCts = new CancellationTokenSource();
        _ = WaitForServerAndConnectAsync(status, serverFlowCts.Token);
    }

    async Task WaitForServerAndConnectAsync(RandomQueueStatus initialStatus, CancellationToken cancellationToken)
    {
        try
        {
            RandomQueueStatus status = initialStatus;

            if (string.IsNullOrWhiteSpace(status.matchId))
                throw new InvalidOperationException("Match server allocation did not include a match id.");

            MatchServerAllocation allocation = new MatchServerAllocation
            {
                success = true,
                allocationId = status.allocationId,
                matchId = status.matchId,
                source = "RandomQueue",
                status = status.serverStatus
            };

            if (string.IsNullOrWhiteSpace(allocation.allocationId))
                throw new InvalidOperationException("Match server allocation did not include an allocation id.");

            SetStatus("Waiting for match server...");

            MatchServerStatus serverStatus = await MatchServerService.EnsureExists().WaitForReadyAsync(
                allocation.matchId,
                allocation.allocationId,
                cancellationToken,
                serverStatusPollIntervalSeconds
            );

            SetStatus("Connecting to match...");
            await MatchConnectionService.EnsureExists().ConnectAsync(serverStatus, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            serverFlowStarted = false;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (this != null)
            {
                SetError(exception.Message);
                serverFlowStarted = false;
            }
        }
    }

    void ApplyGameSession(RandomQueueStatus status)
    {
        string roomId = status.match != null ? status.match.roomId : string.Empty;

        if (string.IsNullOrWhiteSpace(roomId))
            roomId = status.matchId;

        GameSession.SetRoom(
            roomId,
            "Random Match",
            mapId,
            status.maxPlayers > 0 ? status.maxPlayers : 4,
            string.IsNullOrWhiteSpace(mapName) ? "Random" : mapName
        );
    }

    void ApplyStatusWithoutRestartingServerFlow(RandomQueueStatus status)
    {
        if (status == null)
            return;

        if (!string.IsNullOrEmpty(status.matchId) && roomIdlbl != null)
            roomIdlbl.text = "Match: " + status.matchId;

        if (status.status == "Accepted")
        {
            SetStatus(string.IsNullOrWhiteSpace(status.serverStatus)
                ? "Match accepted. Waiting for all players..."
                : FormatServerStatus(status.serverStatus));
            ApplyMatchedPlayers(status.match);
        }
    }

    static string FormatServerStatus(string serverStatus)
    {
        if (string.IsNullOrWhiteSpace(serverStatus))
            return "Waiting for match server...";

        if (string.Equals(serverStatus, "WaitingForServer", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(serverStatus, "Requested", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(serverStatus, "Launching", StringComparison.OrdinalIgnoreCase))
        {
            return "Waiting for match server...";
        }

        return "Server " + serverStatus + "...";
    }

    void SetStatus(string message)
    {
        if (statuslbl != null)
            statuslbl.text = message;
    }

    void SetError(string message)
    {
        polling = false;
        SetStatus(string.IsNullOrWhiteSpace(message) ? "Queue failed. Try again." : message);
        if (cancelbtn != null)
            cancelbtn.interactable = true;
    }

    static Color HexToColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}
