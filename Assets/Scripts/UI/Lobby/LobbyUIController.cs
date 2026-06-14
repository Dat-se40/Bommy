using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyUIController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string characterSelectSceneName = "CharacterSelect";
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Create Room Dialog")]
    [SerializeField] private GameObject createRoomDialog;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_Dropdown mapDropdown;
    [SerializeField] private TMP_Dropdown maxPlayersDropdown;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Current Room UI")]
    [SerializeField] private TMP_Text currentRoomNamelbl;
    [SerializeField] private TMP_Text currentRoomPlayerslbl;
    [SerializeField] private TMP_Text currentRoomMaplbl;

    private void Start()
    {
        if (createRoomDialog != null)
            createRoomDialog.SetActive(false);

        SetCurrentRoomEmpty();
    }

    public void OpenCreateRoomDialog()
    {
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

        if (maxPlayersDropdown != null)
            maxPlayersDropdown.value = 2;

        if (passwordInput != null)
            passwordInput.text = "";
    }

    public void CloseCreateRoomDialog()
    {
        if (createRoomDialog != null)
            createRoomDialog.SetActive(false);
    }

    public void CreateRoom()
    {
        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
            roomName = "My Awesome Room";

        string mapName = mapDropdown.options[mapDropdown.value].text;
        int maxPlayers = GetMaxPlayers();

        if (currentRoomNamelbl != null)
            currentRoomNamelbl.text = "ROOM: " + roomName;

        if (currentRoomPlayerslbl != null)
            currentRoomPlayerslbl.text = "Players: 1/" + maxPlayers;

        if (currentRoomMaplbl != null)
            currentRoomMaplbl.text = "Map: " + mapName;

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
        if (maxPlayersDropdown == null)
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
        if (currentRoomNamelbl != null)
            currentRoomNamelbl.text = "ROOM: -";

        if (currentRoomPlayerslbl != null)
            currentRoomPlayerslbl.text = "Players: -";

        if (currentRoomMaplbl != null)
            currentRoomMaplbl.text = "Map: -";
    }
}
