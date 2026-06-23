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

    [Header("Inputs")]
    [SerializeField] private TMP_InputField roomNameInput;
    // TODO: Làm on value changed cho cái này
    [SerializeField] private TMP_Dropdown mapDropdown;
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

    private string pendingRoomId;
    private string currentRoomId;

    private void Start()
    {
        if (createRoomDialogOverlay != null)
            createRoomDialogOverlay.SetActive(false);
        else if (createRoomDialog != null)
            createRoomDialog.SetActive(false);

        SetupLobbyButtons();
        InitializeFriendsFeature();

        SetCurrentRoomEmpty();
    }

    private void SetupLobbyButtons()
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
            SetFriendsStatus("Room ID is empty.");
            return;
        }

        JoinRoomById(roomId);
    }

    /// <summary>
    /// Join phòng bằng Room ID.
    /// Hiện tại là flow UI demo, sau này thay bằng request lên lobby server/backend.
    /// </summary>
    private void JoinRoomById(string roomId)
    {
        currentRoomId = roomId;

        if (currentRoomNamelbl != null)
            currentRoomNamelbl.text = "ROOM: " + roomId;

        if (currentRoomIdlbl != null)
            currentRoomIdlbl.text = "ID: " + currentRoomId;

        if (currentRoomPlayerslbl != null)
            currentRoomPlayerslbl.text = "Players: ?";

        if (currentRoomMaplbl != null)
            currentRoomMaplbl.text = "Map: -";

        SetFriendsStatus("Joined room " + currentRoomId + ".");
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }


    public void OpenCreateRoomDialog()
    {
        pendingRoomId = GenerateRoomId();

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
            maxPlayersDropdown.value = Mathf.Clamp(2, 0, maxPlayersDropdown.options.Count - 1);

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
            pendingRoomId = GenerateRoomId();

        string roomName = roomNameInput != null
            ? roomNameInput.text.Trim()
            : "";


        if (string.IsNullOrEmpty(roomName))
            roomName = "My Awesome Room";

        string mapName = mapDropdown.options[mapDropdown.value].text;
        int maxPlayers = GetMaxPlayers();

        currentRoomId = pendingRoomId;


        if (currentRoomNamelbl != null)
            currentRoomNamelbl.text = "ROOM: " + roomName;

        if (currentRoomIdlbl != null)
            currentRoomIdlbl.text = "ID: " + currentRoomId;

        if (currentRoomPlayerslbl != null)
            currentRoomPlayerslbl.text = "Players: 1/" + maxPlayers;

        if (currentRoomMaplbl != null)
            currentRoomMaplbl.text = "Map: " + mapName;
        SetFriendsStatus("Created room " + currentRoomId + ".");

        CloseCreateRoomDialog();
    }

    public void ChooseCharacter()
    {
        PlayerPrefs.SetString("CharacterSelectMode", "Lobby");
        PlayerPrefs.Save();

        SceneManager.LoadScene(characterSelectSceneName);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private int GetMaxPlayers()
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

    private void SetCurrentRoomEmpty()
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

    /// <summary>
    /// Tạo mã phòng ngắn để người chơi có thể mời hoặc join bằng ID.
    /// </summary>
    private string GenerateRoomId()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        char[] id = new char[4];

        for (int i = 0; i < id.Length; i++)
        {
            int randomIndex = Random.Range(0, chars.Length);
            id[i] = chars[randomIndex];
        }

        return new string(id);
    }
    // TODO: đi submit cái mấy cái input


}
