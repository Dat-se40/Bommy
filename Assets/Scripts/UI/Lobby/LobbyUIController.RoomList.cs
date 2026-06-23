using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class LobbyUIController
{
    [Header("Room List")]
    [SerializeField] private Transform roomListContent;
    [SerializeField] private LobbyRoomRowUI roomRowTemplate;
    [SerializeField] private TMP_Text roomCountlbl;
    [SerializeField] private Button refreshRoomListbtn;

    [Header("Join Password Dialog (optional)")]
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

        RefreshRoomList();
    }

    void RefreshRoomList()
    {
        LobbyManager.EnsureExists().RequestRoomList();
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

        if (room.isPrivate)
        {
            pendingJoinRoom = room;
            OpenPasswordDialog();
            return;
        }

        LobbyManager.EnsureExists().RequestJoinRoom(new JoinRoomRequest { roomId = room.roomId });
    }

    void OpenPasswordDialog()
    {
        if (passwordDialog == null)
        {
            LobbyManager.EnsureExists().RequestJoinRoom(new JoinRoomRequest
            {
                roomId = pendingJoinRoom.roomId,
                password = ""
            });
            return;
        }

        passwordDialog.SetActive(true);

        if (joinPasswordInput != null)
            joinPasswordInput.text = "";

        if (passwordErrorText != null)
            passwordErrorText.text = "";
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

        string password = joinPasswordInput != null ? joinPasswordInput.text : "";

        LobbyManager.EnsureExists().RequestJoinRoom(new JoinRoomRequest
        {
            roomId = pendingJoinRoom.roomId,
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
            return;
        }

        ApplyCurrentRoomUi(room);
        CloseCreateRoomDialog();
        ClosePasswordDialog();
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
}
