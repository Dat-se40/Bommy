using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class LobbyUIController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string characterSelectSceneName = "CharacterSelect";
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Status")]
    [SerializeField] private TMP_Text lobbyStatuslbl;

    [Header("Create Room Dialog")]
    [SerializeField] private GameObject createRoomDialog;
    [SerializeField] private TMP_Text generatedRoomIdlbl;
    [SerializeField] private GameObject createRoomDialogOverlay;
    [SerializeField] private Button confirmCreateRoombtn;
    [SerializeField] private Button cancelCreateRoombtn;

    [Header("Header")]
    [SerializeField] private TMP_InputField roomIdInput;
    [SerializeField] private Button joinByRoomIdbtn;
    [SerializeField] private Button friendsbtn;
    [SerializeField] private Button backbtn;

    [Header("Create Room Inputs")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_Dropdown mapDropdown;
    [SerializeField] private LobbyMapOption[] mapOptions;
    [SerializeField] private TMP_Dropdown maxPlayersDropdown;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Current Room UI")]
    [SerializeField] private TMP_Text currentRoomNamelbl;
    [SerializeField] private TMP_Text currentRoomIdlbl;
    [SerializeField] private TMP_Text currentRoomPlayerslbl;
    [SerializeField] private TMP_Text currentRoomMaplbl;

    [Header("Room Action Buttons")]
    [SerializeField] private Button createRoombtn;
    [SerializeField] private Button chooseCharbtn;
    [SerializeField] private Button startbtn;

    string pendingRoomId;
    string currentRoomId;

    void Start()
    {
        LobbyManager.EnsureExists();
        BindLobbyManagerEvents();

        if (createRoomDialogOverlay != null)
            createRoomDialogOverlay.SetActive(false);
        else if (createRoomDialog != null)
            createRoomDialog.SetActive(false);

        SetupLobbyButtons();
        InitializeMapDropdown();
        InitializeRoomListFeature();
        InitializeFriendsFeature();

        SetCurrentRoomEmpty();
    }

    private void BindLobbyManagerEvents()
    {
        LobbyManager manager = LobbyManager.EnsureExists();

        manager.CurrentRoomChanged -= OnCurrentRoomChanged;
        manager.CurrentRoomChanged += OnCurrentRoomChanged;

        manager.RoomsListed -= OnRoomsListed;
        manager.RoomsListed += OnRoomsListed;

        manager.FriendsListed -= OnFriendsListed;
        manager.FriendsListed += OnFriendsListed;

        manager.FriendRequestsListed -= OnFriendRequestsListed;
        manager.FriendRequestsListed += OnFriendRequestsListed;

        manager.OperationFailed -= OnLobbyOperationFailed;
        manager.OperationFailed += OnLobbyOperationFailed;
    }

    void SetupLobbyButtons()
    {
        if (joinByRoomIdbtn != null)
        {
            joinByRoomIdbtn.onClick.RemoveAllListeners();
            joinByRoomIdbtn.onClick.AddListener(JoinRoomFromInput);
        }

        if (roomIdInput != null)
        {
            roomIdInput.onSubmit.RemoveAllListeners();
            roomIdInput.onSubmit.AddListener(_ => JoinRoomFromInput());
        }

        if (friendsbtn != null)
        {
            friendsbtn.onClick.RemoveAllListeners();
            friendsbtn.onClick.AddListener(OpenFriendsDialog);
        }

        if (backbtn != null)
        {
            backbtn.onClick.RemoveAllListeners();
            backbtn.onClick.AddListener(BackToMainMenu);
        }

        if (createRoombtn != null)
        {
            createRoombtn.onClick.RemoveAllListeners();
            createRoombtn.onClick.AddListener(OpenCreateRoomDialog);
        }

        if (confirmCreateRoombtn != null)
        {
            confirmCreateRoombtn.onClick.RemoveAllListeners();
            confirmCreateRoombtn.onClick.AddListener(CreateRoom);
        }

        if (cancelCreateRoombtn != null)
        {
            cancelCreateRoombtn.onClick.RemoveAllListeners();
            cancelCreateRoombtn.onClick.AddListener(CloseCreateRoomDialog);
        }

        if (chooseCharbtn != null)
        {
            chooseCharbtn.onClick.RemoveAllListeners();
            chooseCharbtn.onClick.AddListener(ChooseCharacter);
        }

        if (startbtn != null)
        {
            startbtn.onClick.RemoveAllListeners();
            startbtn.onClick.AddListener(StartGame);
        }
    }

    public void JoinRoomFromInput()
    {
        if (roomIdInput == null)
            return;

        string roomId = roomIdInput.text.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(roomId))
        {
            SetLobbyStatus("Room ID is empty.");
            return;
        }

        LobbyManager.EnsureExists().RequestJoinRoomByCode(new JoinRoomByCodeRequest { roomCode = roomId });
    }

    public void BackToMainMenu()
    {
        LobbyManager.Instance?.ClearCurrentRoom();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OpenCreateRoomDialog()
    {
        SoundManager.Instance?.PlayOpenDialog();

        pendingRoomId = LobbyManager.GenerateRoomCodePreview();

        if (generatedRoomIdlbl != null)
            generatedRoomIdlbl.text = "ROOM ID: " + pendingRoomId;

        if (createRoomDialogOverlay != null)
            createRoomDialogOverlay.SetActive(true);

        if (createRoomDialog != null)
            createRoomDialog.SetActive(true);

        if (roomNameInput != null)
        {
            roomNameInput.text = "My Awesome Room";
            roomNameInput.Select();
            roomNameInput.ActivateInputField();
        }

        if (mapDropdown != null)
            mapDropdown.value = 0;

        if (maxPlayersDropdown != null && maxPlayersDropdown.options.Count > 0)
            maxPlayersDropdown.value = Mathf.Clamp(1, 0, maxPlayersDropdown.options.Count - 1);

        if (passwordInput != null)
            passwordInput.text = "";
    }

    public void CloseCreateRoomDialog()
    {
        if (createRoomDialogOverlay != null)
        {
            createRoomDialogOverlay.SetActive(false);
            return;
        }

        if (createRoomDialog != null)
            createRoomDialog.SetActive(false);
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(pendingRoomId))
            pendingRoomId = LobbyManager.GenerateRoomCodePreview();

        string roomName = roomNameInput != null ? roomNameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(roomName))
            roomName = "My Awesome Room";

        ResolveMapSelection(out int mapId, out string mapName);

        var request = new CreateRoomRequest
        {
            roomName = roomName,
            mapId = mapId,
            mapName = mapName,
            maxPlayers = GetMaxPlayers(),
            password = passwordInput != null ? passwordInput.text : "",
            preferredRoomId = pendingRoomId
        };

        LobbyManager.EnsureExists().RequestCreateRoom(request);
    }

    public void ChooseCharacter()
    {
        PlayerPrefs.SetString("CharacterSelectMode", "Lobby");
        PlayerPrefs.Save();
        SceneManager.LoadScene(characterSelectSceneName);
    }

    public void StartGame()
    {
        LobbyManager manager = LobbyManager.EnsureExists();

        if (!manager.TryStartMatch(out string error))
        {
            SetLobbyStatus(error);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    void InitializeMapDropdown()
    {
        if (mapDropdown == null || mapOptions == null || mapOptions.Length == 0)
            return;

        var options = new List<TMP_Dropdown.OptionData>(mapOptions.Length);

        for (int i = 0; i < mapOptions.Length; i++)
            options.Add(new TMP_Dropdown.OptionData(GetMapDisplayName(i)));

        mapDropdown.ClearOptions();
        mapDropdown.AddOptions(options);
        mapDropdown.value = 0;
        mapDropdown.RefreshShownValue();
    }

    string GetMapDisplayName(int index)
    {
        if (mapOptions == null || index < 0 || index >= mapOptions.Length)
            return "Map " + (index + 1);

        string name = mapOptions[index].displayName;
        return string.IsNullOrWhiteSpace(name) ? "Map " + (index + 1) : name.Trim();
    }

    void ResolveMapSelection(out int mapId, out string mapName)
    {
        mapId = LobbyApiContracts.DefaultMapId;
        mapName = "Classic Garden";

        if (mapOptions == null || mapOptions.Length == 0)
            return;

        int index = mapDropdown != null ? mapDropdown.value : 0;
        index = Mathf.Clamp(index, 0, mapOptions.Length - 1);

        mapId = index + 1;
        mapName = GetMapDisplayName(index);
    }

    int GetMaxPlayers()
    {
        if (maxPlayersDropdown == null || maxPlayersDropdown.options.Count == 0)
            return 4;

        string option = maxPlayersDropdown.options[maxPlayersDropdown.value].text;

        if (option.StartsWith("2"))
            return 2;

        if (option.StartsWith("3"))
            return 3;

        return 4;
    }

    void SetCurrentRoomEmpty()
    {
        currentRoomId = "";

        if (currentRoomNamelbl != null)
            currentRoomNamelbl.text = "ROOM: -";

        if (currentRoomIdlbl != null)
            currentRoomIdlbl.text = "ID: -";

        if (currentRoomPlayerslbl != null)
            currentRoomPlayerslbl.text = "Players: -";

        if (currentRoomMaplbl != null)
            currentRoomMaplbl.text = "Map: -";
    }

    void OnDestroy()
    {
        if (LobbyManager.Instance == null)
            return;

        LobbyManager.Instance.CurrentRoomChanged -= OnCurrentRoomChanged;
        LobbyManager.Instance.RoomsListed -= OnRoomsListed;
        LobbyManager.Instance.OperationFailed -= OnLobbyOperationFailed;
        LobbyManager.Instance.FriendsListed -= OnFriendsListed;
        LobbyManager.Instance.FriendRequestsListed -= OnFriendRequestsListed;
        LobbyManager.Instance.OperationFailed -= OnLobbyOperationFailed;
    }

    void SetLobbyStatus(string message)
    {
        if (lobbyStatuslbl != null)
            lobbyStatuslbl.text = message;

        SetFriendsStatus(message);
    }
}
