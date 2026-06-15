using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class LobbyUIController
{
    [System.Serializable]
    private class FriendData
    {
        public string friendId;
        public string displayName;
        public bool online;
        public bool isSteamFriend;
        public string currentRoomId;
    }

    [System.Serializable]
    private class FriendRequestData
    {
        public string friendId;
        public string displayName;
        public bool isSteamFriend;
    }

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

    private readonly List<FriendData> friends = new();
    private readonly List<FriendRequestData> friendRequests = new();

    private readonly List<FriendRowUI> spawnedFriendRows = new();
    private readonly List<FriendRequestRowUI> spawnedRequestRows = new();

    private bool steamConnected = true;

    /// <summary>
    /// Khởi tạo UI Friends dialog khi vào Lobby.
    /// </summary>
    private void InitializeFriendsFeature()
    {
        if (friendsDialogOverlay != null)
            friendsDialogOverlay.SetActive(false);

        if (friendRowTemplate != null)
            friendRowTemplate.gameObject.SetActive(false);

        if (requestRowTemplate != null)
            requestRowTemplate.gameObject.SetActive(false);

        SetupFriendsButtons();
        SetupFriendSearch();
        SeedFriendDemoData();
        RefreshSteamStatus();
    }

    private void SetupFriendSearch()
    {
        if (friendSearchInput != null)
        {
            friendSearchInput.onSubmit.RemoveAllListeners();
            friendSearchInput.onSubmit.AddListener(_ => SearchFriends());
        }
    }


    private void SetupFriendsButtons()
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
        if (friendsDialogOverlay != null)
            friendsDialogOverlay.SetActive(true);

        ShowFriendsTab();
        RefreshSteamStatus();
        RefreshFriendList();
        RefreshRequestList();

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

    // DEMO, sau này thay bằng backend/Steam data.
    private void SeedFriendDemoData()
    {
        if (friends.Count == 0)
        {
            friends.Add(new FriendData
            {
                friendId = "1001",
                displayName = "MimiFan",
                online = true,
                isSteamFriend = true,
                currentRoomId = "RX45"
            });

            friends.Add(new FriendData
            {
                friendId = "1002",
                displayName = "BomberCat",
                online = true,
                isSteamFriend = true,
                currentRoomId = ""
            });

            friends.Add(new FriendData
            {
                friendId = "2001",
                displayName = "LocalBuddy",
                online = true,
                isSteamFriend = false,
                currentRoomId = "MM88"
            });

            friends.Add(new FriendData
            {
                friendId = "3001",
                displayName = "OfflineDog",
                online = false,
                isSteamFriend = false,
                currentRoomId = ""
            });
        }

        if (friendRequests.Count == 0)
        {
            friendRequests.Add(new FriendRequestData
            {
                friendId = "9001",
                displayName = "NewBomber",
                isSteamFriend = false
            });

            friendRequests.Add(new FriendRequestData
            {
                friendId = "STEAM42",
                displayName = "SteamGuest",
                isSteamFriend = true
            });
        }
    }

    private void RefreshFriendList()
    {
        ClearFriendRows();

        if (friendListContent == null || friendRowTemplate == null)
        {
            if (friendsCountlbl != null)
                friendsCountlbl.text = "0/" + friends.Count + " FRIENDS";

            return;
        }

        string keyword = friendSearchInput != null
            ? friendSearchInput.text.Trim().ToLowerInvariant()
            : "";

        int shownCount = 0;

        for (int i = 0; i < friends.Count; i++)
        {
            FriendData friend = friends[i];

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
            friendsCountlbl.text = shownCount + "/" + friends.Count + " FRIENDS";

    }

    /// <summary>
    /// Tìm friend theo tên hoặc ID trong danh sách bạn bè.
    /// </summary>
    public void SearchFriends()
    {
        RefreshFriendList();
    }


    private void ClearFriendRows()
    {
        for (int i = 0; i < spawnedFriendRows.Count; i++)
        {
            if (spawnedFriendRows[i] != null)
                Destroy(spawnedFriendRows[i].gameObject);
        }

        spawnedFriendRows.Clear();
    }


    private void RefreshRequestList()
    {
        ClearRequestRows();

        if (requestListContent == null || requestRowTemplate == null)
        {
            if (requestsCountlbl != null)
                requestsCountlbl.text = friendRequests.Count + " REQUESTS";

            return;
        }

        if (requestsCountlbl != null)
            requestsCountlbl.text = friendRequests.Count + " REQUESTS";

        for (int i = 0; i < friendRequests.Count; i++)
        {
            FriendRequestData request = friendRequests[i];

            FriendRequestRowUI row = Instantiate(requestRowTemplate, requestListContent);
            row.gameObject.SetActive(true);

            row.Setup(
                request.friendId,
                request.displayName,
                request.isSteamFriend,
                AcceptFriendRequest,
                DeclineFriendRequest
            );

            spawnedRequestRows.Add(row);
        }
    }

    private void ClearRequestRows()
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

        if (FindFriend(id) != null)
        {
            SetFriendsStatus("Friend already exists.");
            return;
        }

        if (FindRequest(id) != null)
        {
            SetFriendsStatus("This player already sent a request.");
            return;
        }

        friendRequests.Insert(0, new FriendRequestData
        {
            friendId = id,
            displayName = "Friend " + id,
            isSteamFriend = false
        });

        friendIdInput.text = "";

        SetFriendsStatus("Friend request created for ID: " + id);
        RefreshRequestList();
    }

    private FriendData FindFriend(string friendId)
    {
        for (int i = 0; i < friends.Count; i++)
        {
            if (friends[i].friendId == friendId)
                return friends[i];
        }

        return null;
    }

    private FriendRequestData FindRequest(string friendId)
    {
        for (int i = 0; i < friendRequests.Count; i++)
        {
            if (friendRequests[i].friendId == friendId)
                return friendRequests[i];
        }

        return null;
    }

    private void AcceptFriendRequest(string friendId)
    {
        for (int i = 0; i < friendRequests.Count; i++)
        {
            if (friendRequests[i].friendId != friendId)
                continue;

            FriendRequestData request = friendRequests[i];

            friends.Insert(0, new FriendData
            {
                friendId = request.friendId,
                displayName = request.displayName,
                online = true,
                isSteamFriend = request.isSteamFriend,
                currentRoomId = ""
            });

            friendRequests.RemoveAt(i);

            SetFriendsStatus("Accepted " + request.displayName + ".");
            RefreshRequestList();
            RefreshFriendList();
            return;
        }

        SetFriendsStatus("Request not found.");
    }

    private void DeclineFriendRequest(string friendId)
    {
        for (int i = 0; i < friendRequests.Count; i++)
        {
            if (friendRequests[i].friendId != friendId)
                continue;

            string displayName = friendRequests[i].displayName;
            friendRequests.RemoveAt(i);

            SetFriendsStatus("Declined " + displayName + ".");
            RefreshRequestList();
            return;
        }

        SetFriendsStatus("Request not found.");
    }

    /// <summary>
    /// Mời friend vào phòng hiện tại.
    /// Steam sẽ nối sau, hiện tại chỉ hiển thị status.
    /// </summary>
    private void InviteFriend(string friendId)
    {
        FriendData friend = FindFriend(friendId);

        if (friend == null)
        {
            SetFriendsStatus("Friend not found.");
            return;
        }

        if (!friend.online)
        {
            SetFriendsStatus(friend.displayName + " is offline.");
            return;
        }

        if (string.IsNullOrEmpty(currentRoomId))
        {
            SetFriendsStatus("Create or join a room first.");
            return;
        }

        if (friend.isSteamFriend)
        {
            if (!IsSteamConnected())
            {
                SetFriendsStatus("Steam is not connected.");
                return;
            }

            // TODO[STEAM]: Gửi lời mời qua Steam overlay/app.
            SetFriendsStatus("Steam invite sent to " + friend.displayName + " for room " + currentRoomId + ".");
            return;
        }

        // TODO[NETWORK]: Gửi invite qua backend/lobby server.
        SetFriendsStatus("Invite sent to " + friend.displayName + " for room " + currentRoomId + ".");
    }

    private void JoinFriendRoom(string friendId)
    {
        FriendData friend = FindFriend(friendId);

        if (friend == null)
        {
            SetFriendsStatus("Friend not found.");
            return;
        }

        if (!friend.online)
        {
            SetFriendsStatus(friend.displayName + " is offline.");
            return;
        }

        if (string.IsNullOrEmpty(friend.currentRoomId))
        {
            SetFriendsStatus(friend.displayName + " is not in a room.");
            return;
        }

        currentRoomId = friend.currentRoomId;

        if (currentRoomNamelbl != null)
            currentRoomNamelbl.text = "ROOM: " + friend.displayName + "'s Room";

        if (currentRoomIdlbl != null)
            currentRoomIdlbl.text = "ID: " + currentRoomId;

        if (currentRoomPlayerslbl != null)
            currentRoomPlayerslbl.text = "Players: ?";

        if (currentRoomMaplbl != null)
            currentRoomMaplbl.text = "Map: -";

        SetFriendsStatus("Joined room " + currentRoomId + " from " + friend.displayName + ".");
    }

    /// <summary>
    /// Cập nhật trạng thái kết nối Steam trên UI.
    /// Steam thật sẽ nối sau.
    /// </summary>
    private void RefreshSteamStatus()
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

    private bool IsSteamConnected()
    {
        // TODO[STEAM]: Thay bằng SteamManager.Initialized hoặc trạng thái Steamworks.
        return steamConnected;
    }

    private void SetFriendsStatus(string message)
    {
        if (friendsStatuslbl != null)
            friendsStatuslbl.text = message;
    }


}
