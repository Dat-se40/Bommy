using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class LobbyUIController
{
    const float RoomListRefreshIntervalSeconds = 2f;

    [Header("Room List")]
    [SerializeField] private Transform roomListContent;
    [SerializeField] private LobbyRoomRowUI roomRowTemplate;
    [SerializeField] private TMP_Text roomCountlbl;
    [SerializeField] private Button refreshRoomListbtn;

    [Header("Join Password Dialog")]
    [SerializeField] private GameObject passwordDialog;
    [SerializeField] private TMP_InputField joinPasswordInput;
    [SerializeField] private TMP_Text passwordErrorText;
    [SerializeField] private Button passwordConfirmbtn;
    [SerializeField] private Button passwordCancelbtn;

    readonly List<LobbyRoomRowUI> spawnedRoomRows = new();
    LobbyRoomDto pendingJoinRoom;

    void InitializeRoomListFeature()
    {
        if (roomRowTemplate != null)
            roomRowTemplate.gameObject.SetActive(false);

        if (passwordDialog != null)
            passwordDialog.SetActive(false);

        if (refreshRoomListbtn != null)
        {
            refreshRoomListbtn.onClick.RemoveAllListeners();
            refreshRoomListbtn.onClick.AddListener(RefreshRoomList);
        }

        if (passwordConfirmbtn != null)
        {
            passwordConfirmbtn.onClick.RemoveAllListeners();
            passwordConfirmbtn.onClick.AddListener(ConfirmJoinWithPassword);
        }

        if (passwordCancelbtn != null)
        {
            passwordCancelbtn.onClick.RemoveAllListeners();
            passwordCancelbtn.onClick.AddListener(ClosePasswordDialog);
        }

        CancelInvoke(nameof(RefreshRoomList));
        InvokeRepeating(nameof(RefreshRoomList), 0f, RoomListRefreshIntervalSeconds);
    }

    void RefreshRoomList()
    {
        LobbyManager manager = LobbyManager.EnsureExists();
        manager.RequestRoomList();
        manager.RequestCurrentRoomRefresh();
    }

    void OnRoomsListed(ListRoomsResponse response)
    {
        ClearRoomRows();

        if (response?.rooms == null || roomListContent == null || roomRowTemplate == null)
        {
            UpdateRoomCountLabel(0, response?.totalCount ?? 0);
            return;
        }

        for (int i = 0; i < response.rooms.Length; i++)
        {
            LobbyRoomDto entry = response.rooms[i];
            LobbyRoomRowUI row = Instantiate(roomRowTemplate, roomListContent);
            row.gameObject.SetActive(true);
            row.Setup(entry, OnRoomRowJoinClicked);
            spawnedRoomRows.Add(row);
        }

        UpdateRoomCountLabel(response.rooms.Length, response.totalCount);
    }

    void UpdateRoomCountLabel(int shown, int total)
    {
        if (roomCountlbl == null)
            return;

        roomCountlbl.text = "SHOWING " + shown + "/" + total + " ROOMS";
    }

    void OnRoomRowJoinClicked(LobbyRoomDto room)
    {
        if (room == null)
            return;

        pendingJoinRoom = room;
        OpenPasswordDialog();
    }

    void OpenPasswordDialog()
    {
        if (pendingJoinRoom == null)
            return;

        if (passwordDialog == null)
        {
            SetLobbyStatus("Join password dialog is not configured.");
            return;
        }

        SoundManager.Instance?.PlayOpenDialog();
        passwordDialog.SetActive(true);

        if (joinPasswordInput != null)
        {
            joinPasswordInput.text = "";
            joinPasswordInput.Select();
            joinPasswordInput.ActivateInputField();
        }

        if (passwordErrorText != null)
            passwordErrorText.text = "Enter room ID " + pendingJoinRoom.roomId + " to join.";
    }

    void ClosePasswordDialog()
    {
        pendingJoinRoom = null;

        if (passwordDialog != null)
            passwordDialog.SetActive(false);
    }

    void ConfirmJoinWithPassword()
    {
        if (pendingJoinRoom == null)
            return;

        string password = joinPasswordInput != null ? joinPasswordInput.text.Trim().ToUpperInvariant() : "";

        LobbyManager.EnsureExists().RequestJoinRoom(new JoinRoomRequest
        {
            roomId = pendingJoinRoom.roomId,
            matchId = pendingJoinRoom.matchId,
            password = password
        });
    }

    void ClearRoomRows()
    {
        for (int i = 0; i < spawnedRoomRows.Count; i++)
        {
            if (spawnedRoomRows[i] != null)
                Destroy(spawnedRoomRows[i].gameObject);
        }

        spawnedRoomRows.Clear();
    }

    void OnCurrentRoomChanged(LobbyRoomDto room)
    {
        if (room == null)
        {
            SetCurrentRoomEmpty();
            ApplyLobbyLaunchLock(false);
            return;
        }

        ApplyCurrentRoomUi(room);
        ApplyLobbyLaunchLock(!string.IsNullOrEmpty(room.status) && room.status == "Starting");
        CloseCreateRoomDialog();
        ClosePasswordDialog();

        if (!string.IsNullOrEmpty(room.status) && room.status != "Open")
            SetLobbyStatus("Room " + room.roomId + " is " + room.status + ".");
        else
            SetLobbyStatus("In room " + room.roomId + ".");
    }

    void OnLobbyOperationFailed(string message)
    {
        if (passwordErrorText != null && passwordDialog != null && passwordDialog.activeSelf)
            passwordErrorText.text = message;

        SetLobbyStatus(message);
    }

    void ApplyCurrentRoomUi(LobbyRoomDto room)
    {
        currentRoomId = room.roomId;

        if (currentRoomNamelbl != null)
            currentRoomNamelbl.text = "ROOM: " + room.roomName;

        if (currentRoomIdlbl != null)
            currentRoomIdlbl.text = "ID: " + room.roomId;

        if (currentRoomPlayerslbl != null)
            currentRoomPlayerslbl.text = "Players: " + room.currentPlayers + "/" + room.maxPlayers;

        if (currentRoomMaplbl != null)
            currentRoomMaplbl.text = "Map: " + room.mapName;
    }

    void ApplyLobbyLaunchLock(bool locked)
    {
        if (joinByRoomIdbtn != null)
            joinByRoomIdbtn.interactable = !locked;

        if (createRoombtn != null)
            createRoombtn.interactable = !locked;

        if (chooseCharbtn != null)
            chooseCharbtn.interactable = !locked;

        if (startbtn != null)
            startbtn.interactable = !locked;

        if (backbtn != null)
            backbtn.interactable = !locked;
    }
}
