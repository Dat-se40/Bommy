using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class LobbyUIController
{
    [Header("Friends Dialog")]
    [SerializeField] private GameObject friendsDialogOverlay;
    [SerializeField] private GameObject friendsPage;
    [SerializeField] private GameObject requestsPage;
    [SerializeField] private TMP_Text friendsStatuslbl;

    [Header("Friends Dialog Buttons")]
    [SerializeField] private Button closeFriendsbtn;
    [SerializeField] private Button friendsTabbtn;
    [SerializeField] private Button requestsTabbtn;
    [SerializeField] private Button searchbtn;
    [SerializeField] private Button addFriendbtn;

    [Header("Steam Status")]
    [SerializeField] private Image steamIcon;
    [SerializeField] private TMP_Text steamStatuslbl;

    [Header("Friends Page")]
    [SerializeField] private TMP_InputField friendSearchInput;
    [SerializeField] private Transform friendListContent;
    [SerializeField] private FriendRowUI friendRowTemplate;
    [SerializeField] private TMP_Text friendsCountlbl;

    [Header("Requests Page")]
    [SerializeField] private TMP_InputField friendIdInput;
    [SerializeField] private TMP_Text requestsCountlbl;
    [SerializeField] private Transform requestListContent;
    [SerializeField] private FriendRequestRowUI requestRowTemplate;

    readonly List<FriendRowUI> spawnedFriendRows = new();
    readonly List<FriendRequestRowUI> spawnedRequestRows = new();
    FriendDto[] cachedFriends = System.Array.Empty<FriendDto>();

    bool steamConnected = true;

    void InitializeFriendsFeature()
    {
        if (friendsDialogOverlay != null)
            friendsDialogOverlay.SetActive(false);

        if (friendRowTemplate != null)
            friendRowTemplate.gameObject.SetActive(false);

        if (requestRowTemplate != null)
            requestRowTemplate.gameObject.SetActive(false);

        SetupFriendsButtons();
        SetupFriendSearch();
        RefreshSteamStatus();
    }

    void SetupFriendSearch()
    {
        if (friendSearchInput != null)
        {
            friendSearchInput.onSubmit.RemoveAllListeners();
            friendSearchInput.onSubmit.AddListener(_ => SearchFriends());
        }
    }

    void SetupFriendsButtons()
    {
        if (closeFriendsbtn != null)
        {
            closeFriendsbtn.onClick.RemoveAllListeners();
            closeFriendsbtn.onClick.AddListener(CloseFriendsDialog);
        }

        if (friendsTabbtn != null)
        {
            friendsTabbtn.onClick.RemoveAllListeners();
            friendsTabbtn.onClick.AddListener(ShowFriendsTab);
        }

        if (requestsTabbtn != null)
        {
            requestsTabbtn.onClick.RemoveAllListeners();
            requestsTabbtn.onClick.AddListener(ShowRequestsTab);
        }

        if (searchbtn != null)
        {
            searchbtn.onClick.RemoveAllListeners();
            searchbtn.onClick.AddListener(SearchFriends);
        }

        if (addFriendbtn != null)
        {
            addFriendbtn.onClick.RemoveAllListeners();
            addFriendbtn.onClick.AddListener(AddFriendById);
        }
    }

    public void OpenFriendsDialog()
    {
        SoundManager.Instance?.PlayOpenDialog();

        if (friendsDialogOverlay != null)
            friendsDialogOverlay.SetActive(true);

        ShowFriendsTab();
        RefreshSteamStatus();

        LobbyManager manager = LobbyManager.EnsureExists();
        manager.RequestFriendsList();
        manager.RequestFriendRequests();

        SetFriendsStatus("Invite friends or join their room.");
    }

    public void CloseFriendsDialog()
    {
        if (friendsDialogOverlay != null)
            friendsDialogOverlay.SetActive(false);
    }

    public void ShowFriendsTab()
    {
        if (friendsPage != null)
            friendsPage.SetActive(true);

        if (requestsPage != null)
            requestsPage.SetActive(false);

        SetFriendsStatus("Invite friends or join their room.");
    }

    public void ShowRequestsTab()
    {
        if (friendsPage != null)
            friendsPage.SetActive(false);

        if (requestsPage != null)
            requestsPage.SetActive(true);

        SetFriendsStatus("Add friends by ID or handle requests.");
    }

    void OnFriendsListed(FriendDto[] friends)
    {
        cachedFriends = friends ?? System.Array.Empty<FriendDto>();
        RefreshFriendList();
    }

    void OnFriendRequestsListed(FriendRequestDto[] requests)
    {
        RefreshRequestList(requests);
    }

    void RefreshFriendList()
    {
        ClearFriendRows();

        if (friendListContent == null || friendRowTemplate == null)
        {
            if (friendsCountlbl != null)
                friendsCountlbl.text = "0/" + cachedFriends.Length + " FRIENDS";

            return;
        }

        string keyword = friendSearchInput != null
            ? friendSearchInput.text.Trim().ToLowerInvariant()
            : "";

        int shownCount = 0;

        for (int i = 0; i < cachedFriends.Length; i++)
        {
            FriendDto friend = cachedFriends[i];

            bool match =
                string.IsNullOrEmpty(keyword) ||
                friend.displayName.ToLowerInvariant().Contains(keyword) ||
                friend.friendId.ToLowerInvariant().Contains(keyword);

            if (!match)
                continue;

            FriendRowUI row = Instantiate(friendRowTemplate, friendListContent);
            row.gameObject.SetActive(true);

            row.Setup(
                friend.friendId,
                friend.displayName,
                friend.username,
                friend.online,
                friend.isSteamFriend,
                friend.currentRoomId,
                InviteFriend,
                JoinFriendRoom
            );

            spawnedFriendRows.Add(row);
            shownCount++;
        }

        if (friendsCountlbl != null)
            friendsCountlbl.text = shownCount + "/" + cachedFriends.Length + " FRIENDS";
    }

    public void SearchFriends()
    {
        RefreshFriendList();
    }

    void ClearFriendRows()
    {
        for (int i = 0; i < spawnedFriendRows.Count; i++)
        {
            if (spawnedFriendRows[i] != null)
                Destroy(spawnedFriendRows[i].gameObject);
        }

        spawnedFriendRows.Clear();
    }

    void RefreshRequestList(FriendRequestDto[] requests)
    {
        ClearRequestRows();

        if (requests == null)
            requests = System.Array.Empty<FriendRequestDto>();

        if (requestListContent == null || requestRowTemplate == null)
        {
            if (requestsCountlbl != null)
                requestsCountlbl.text = requests.Length + " REQUESTS";

            return;
        }

        if (requestsCountlbl != null)
            requestsCountlbl.text = requests.Length + " REQUESTS";

        for (int i = 0; i < requests.Length; i++)
        {
            FriendRequestDto request = requests[i];

            FriendRequestRowUI row = Instantiate(requestRowTemplate, requestListContent);
            row.gameObject.SetActive(true);

            row.Setup(
                request.friendId,
                request.displayName,
                request.username,
                request.isSteamFriend,
                AcceptFriendRequest,
                DeclineFriendRequest
            );

            spawnedRequestRows.Add(row);
        }
    }

    void ClearRequestRows()
    {
        for (int i = 0; i < spawnedRequestRows.Count; i++)
        {
            if (spawnedRequestRows[i] != null)
                Destroy(spawnedRequestRows[i].gameObject);
        }

        spawnedRequestRows.Clear();
    }

    public void AddFriendById()
    {
        if (friendIdInput == null)
            return;

        string id = friendIdInput.text.Trim();

        if (string.IsNullOrEmpty(id))
        {
            SetFriendsStatus("Friend ID is empty.");
            return;
        }

        LobbyManager.EnsureExists().RequestAddFriend(new AddFriendRequest { friendId = id });
        friendIdInput.text = "";
        SetFriendsStatus("Friend request sent for ID: " + id);
    }

    void AcceptFriendRequest(string friendId)
    {
        LobbyManager.EnsureExists().RequestAcceptFriend(friendId);
        SetFriendsStatus("Accepted friend request.");
    }

    void DeclineFriendRequest(string friendId)
    {
        LobbyManager.EnsureExists().RequestDeclineFriend(friendId);
        SetFriendsStatus("Declined friend request.");
    }

    void InviteFriend(string friendId)
    {
        LobbyManager manager = LobbyManager.EnsureExists();

        if (!manager.HasCurrentRoom)
        {
            SetFriendsStatus("Create or join a room first.");
            return;
        }

        manager.RequestInviteFriend(new InviteFriendRequest
        {
            friendId = friendId,
            roomId = manager.CurrentRoom.roomId
        });

        SetFriendsStatus("Invite sent.");
    }

    void JoinFriendRoom(string friendId)
    {
        LobbyManager.EnsureExists().RequestJoinFriendRoom(friendId);
    }

    void RefreshSteamStatus()
    {
        bool connected = IsSteamConnected();

        if (steamIcon != null)
        {
            Color color = steamIcon.color;
            color.a = connected ? 1f : 0.35f;
            steamIcon.color = color;
        }

        if (steamStatuslbl != null)
            steamStatuslbl.text = connected ? "STEAM ON" : "STEAM OFF";
    }

    bool IsSteamConnected()
    {
        return steamConnected;
    }

    void SetFriendsStatus(string message)
    {
        if (friendsStatuslbl != null)
            friendsStatuslbl.text = message;
    }
}
