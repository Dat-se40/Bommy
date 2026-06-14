using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyBrowserUIController : MonoBehaviour
{
    [System.Serializable]
    public class LobbyEntry
    {
        public string roomName;
        public int currentPlayers;
        public int maxPlayers;
        public int ping;
        public string region;
        public bool isPrivate;
        public string password;
        public string mapName;

        public LobbyEntry(string roomName, int currentPlayers, int maxPlayers, int ping, string region, bool isPrivate, string password, string mapName)
        {
            this.roomName = roomName;
            this.currentPlayers = currentPlayers;
            this.maxPlayers = maxPlayers;
            this.ping = ping;
            this.region = region;
            this.isPrivate = isPrivate;
            this.password = password;
            this.mapName = mapName;
        }
    }

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string lobbySceneName = "Lobby";

    [Header("Navigation")]
    [SerializeField] private Button backBtn;

    [Header("Lobby List Browser")]
    [SerializeField] private Transform lobbyListContainer;
    [SerializeField] private GameObject lobbyItemPrefab;

    [Header("Selected Lobby Info Panel")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private TMP_Text detailsNameText;
    [SerializeField] private TMP_Text detailsPlayersText;
    [SerializeField] private TMP_Text detailsMapText;
    [SerializeField] private TMP_Text detailsRegionText;
    [SerializeField] private TMP_Text detailsPingText;
    [SerializeField] private TMP_Text detailsPrivacyText;

    [Header("Action Buttons")]
    [SerializeField] private Button joinBtn;
    [SerializeField] private Button createBtn;

    [Header("Create Room Dialog")]
    [SerializeField] private GameObject createDialog;
    [SerializeField] private TMP_InputField createRoomNameInput;
    [SerializeField] private TMP_Dropdown createMapDropdown;
    [SerializeField] private TMP_Dropdown createMaxPlayersDropdown;
    [SerializeField] private TMP_InputField createPasswordInput;
    [SerializeField] private Button createConfirmBtn;
    [SerializeField] private Button createCancelBtn;

    [Header("Password Dialog")]
    [SerializeField] private GameObject passwordDialog;
    [SerializeField] private TMP_InputField joinPasswordInput;
    [SerializeField] private TMP_Text passwordErrorText;
    [SerializeField] private Button passwordConfirmBtn;
    [SerializeField] private Button passwordCancelBtn;

    [Header("Pagination")]
    [SerializeField] private Button prevPageBtn;
    [SerializeField] private Button nextPageBtn;
    [SerializeField] private TMP_Text pageText;

    private List<LobbyEntry> allLobbies = new List<LobbyEntry>();
    private List<GameObject> instantiatedItems = new List<GameObject>();
    private int selectedLobbyIndex = -1;
    private int currentPage = 0;
    private const int ItemsPerPage = 5;

    private void Start()
    {
        InitializeMockLobbies();
        SetupListeners();
        
        if (createDialog != null) createDialog.SetActive(false);
        if (passwordDialog != null) passwordDialog.SetActive(false);

        currentPage = 0;
        selectedLobbyIndex = -1;
        
        UpdateSelectedLobbyUI();
        RenderLobbies();
    }

    private void InitializeMockLobbies()
    {
        allLobbies.Clear();
        allLobbies.Add(new LobbyEntry("SG Casual Play", 1, 4, 45, "Singapore", false, "", "Classic Garden"));
        allLobbies.Add(new LobbyEntry("VN PRO ONLY (123)", 3, 4, 30, "Singapore", true, "123", "Classic Garden"));
        allLobbies.Add(new LobbyEntry("US West Fun Match", 2, 4, 150, "US West", false, "", "Classic Garden"));
        allLobbies.Add(new LobbyEntry("Europe Chill Room", 1, 4, 210, "Europe", false, "", "Classic Garden"));
        allLobbies.Add(new LobbyEntry("Japan Dev Test", 4, 4, 90, "Japan", false, "", "Classic Garden"));
        allLobbies.Add(new LobbyEntry("Secret Room (dev)", 2, 4, 52, "Singapore", true, "dev", "Classic Garden"));
        allLobbies.Add(new LobbyEntry("Mock Lobby 7", 1, 2, 80, "US East", false, "", "Classic Garden"));
        allLobbies.Add(new LobbyEntry("Mock Lobby 8", 2, 3, 110, "Australia", false, "", "Classic Garden"));
    }

    private void SetupListeners()
    {
        if (backBtn != null) backBtn.onClick.AddListener(OnBackClicked);
        if (joinBtn != null) joinBtn.onClick.AddListener(OnJoinClicked);
        if (createBtn != null) createBtn.onClick.AddListener(OnCreateClicked);

        if (createConfirmBtn != null) createConfirmBtn.onClick.AddListener(OnConfirmCreateClicked);
        if (createCancelBtn != null) createCancelBtn.onClick.AddListener(() => createDialog.SetActive(false));

        if (passwordConfirmBtn != null) passwordConfirmBtn.onClick.AddListener(OnConfirmPasswordClicked);
        if (passwordCancelBtn != null) passwordCancelBtn.onClick.AddListener(() => passwordDialog.SetActive(false));

        if (prevPageBtn != null) prevPageBtn.onClick.AddListener(OnPrevPageClicked);
        if (nextPageBtn != null) nextPageBtn.onClick.AddListener(OnNextPageClicked);
    }

    private void RenderLobbies()
    {
        // Clear previous items
        foreach (var item in instantiatedItems)
        {
            Destroy(item);
        }
        instantiatedItems.Clear();

        int startIndex = currentPage * ItemsPerPage;
        int endIndex = Mathf.Min(startIndex + ItemsPerPage, allLobbies.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            int index = i; // local copy
            LobbyEntry lobby = allLobbies[index];
            GameObject item = Instantiate(lobbyItemPrefab, lobbyListContainer);
            instantiatedItems.Add(item);

            // Bind values
            var nameText = item.transform.Find("RoomName").GetComponent<TMP_Text>();
            var playersText = item.transform.Find("Players").GetComponent<TMP_Text>();
            var pingText = item.transform.Find("Ping").GetComponent<TMP_Text>();
            var regionText = item.transform.Find("Region").GetComponent<TMP_Text>();
            var privacyText = item.transform.Find("Privacy").GetComponent<TMP_Text>();
            var selectBtn = item.GetComponent<Button>();

            if (nameText != null) nameText.text = lobby.roomName;
            if (playersText != null) playersText.text = $"{lobby.currentPlayers}/{lobby.maxPlayers}";
            if (pingText != null) pingText.text = $"{lobby.ping}ms";
            if (regionText != null) regionText.text = lobby.region;
            if (privacyText != null) privacyText.text = lobby.isPrivate ? "Private" : "Public";

            // Outline or highlight if selected
            var outline = item.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = index == selectedLobbyIndex;
            }

            if (selectBtn != null)
            {
                selectBtn.onClick.RemoveAllListeners();
                selectBtn.onClick.AddListener(() => OnLobbySelected(index));
            }
        }

        UpdatePaginationUI();
    }

    private void OnLobbySelected(int index)
    {
        selectedLobbyIndex = index;
        RenderLobbies();
        UpdateSelectedLobbyUI();
    }

    private void UpdateSelectedLobbyUI()
    {
        bool hasSelection = selectedLobbyIndex >= 0 && selectedLobbyIndex < allLobbies.Count;
        
        if (detailsPanel != null) detailsPanel.SetActive(hasSelection);
        if (joinBtn != null) joinBtn.interactable = hasSelection;

        if (hasSelection)
        {
            LobbyEntry lobby = allLobbies[selectedLobbyIndex];
            if (detailsNameText != null) detailsNameText.text = lobby.roomName;
            if (detailsPlayersText != null) detailsPlayersText.text = $"{lobby.currentPlayers}/{lobby.maxPlayers}";
            if (detailsMapText != null) detailsMapText.text = lobby.mapName;
            if (detailsRegionText != null) detailsRegionText.text = lobby.region;
            if (detailsPingText != null) detailsPingText.text = $"{lobby.ping}ms";
            if (detailsPrivacyText != null) detailsPrivacyText.text = lobby.isPrivate ? "Private" : "Public";
        }
    }

    private void UpdatePaginationUI()
    {
        int totalPages = Mathf.CeilToInt((float)allLobbies.Count / ItemsPerPage);
        if (totalPages == 0) totalPages = 1;

        if (pageText != null)
        {
            pageText.text = $"{currentPage + 1}/{totalPages}";
        }

        if (prevPageBtn != null) prevPageBtn.interactable = currentPage > 0;
        if (nextPageBtn != null) nextPageBtn.interactable = currentPage < totalPages - 1;
    }

    private void OnPrevPageClicked()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RenderLobbies();
        }
    }

    private void OnNextPageClicked()
    {
        int totalPages = Mathf.CeilToInt((float)allLobbies.Count / ItemsPerPage);
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            RenderLobbies();
        }
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnCreateClicked()
    {
        if (createDialog != null)
        {
            createDialog.SetActive(true);
            if (createRoomNameInput != null) createRoomNameInput.text = "Casual Room";
            if (createPasswordInput != null) createPasswordInput.text = "";
        }
    }

    private void OnConfirmCreateClicked()
    {
        string roomName = createRoomNameInput != null ? createRoomNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(roomName)) roomName = "Casual Room";

        string mapName = "Classic Garden";
        if (createMapDropdown != null && createMapDropdown.options.Count > 0)
        {
            mapName = createMapDropdown.options[createMapDropdown.value].text;
        }

        int maxPlayers = 4;
        if (createMaxPlayersDropdown != null)
        {
            string option = createMaxPlayersDropdown.options[createMaxPlayersDropdown.value].text;
            int.TryParse(option, out maxPlayers);
            if (maxPlayers <= 0) maxPlayers = 4;
        }

        string password = createPasswordInput != null ? createPasswordInput.text : "";
        bool isPrivate = !string.IsNullOrEmpty(password);

        // Update GameSession settings
        GameSession.SetRoom(roomName, mapName, maxPlayers);
        
        // Also save isPrivate / password to PlayerPrefs for verification later if needed
        PlayerPrefs.SetString("CurrentRoomPassword", password);
        PlayerPrefs.SetInt("CurrentRoomIsPrivate", isPrivate ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[FLOW:SETUP] LobbyBrowser created room {roomName}, map {mapName}, max {maxPlayers}. Transitioning to Lobby scene.");
        SceneManager.LoadScene(lobbySceneName);
    }

    private void OnJoinClicked()
    {
        if (selectedLobbyIndex < 0 || selectedLobbyIndex >= allLobbies.Count) return;

        LobbyEntry lobby = allLobbies[selectedLobbyIndex];

        if (lobby.isPrivate)
        {
            if (passwordDialog != null)
            {
                passwordDialog.SetActive(true);
                if (joinPasswordInput != null) joinPasswordInput.text = "";
                if (passwordErrorText != null) passwordErrorText.text = "";
            }
        }
        else
        {
            JoinLobbyDirectly(lobby);
        }
    }

    private void OnConfirmPasswordClicked()
    {
        if (selectedLobbyIndex < 0 || selectedLobbyIndex >= allLobbies.Count) return;
        LobbyEntry lobby = allLobbies[selectedLobbyIndex];

        string enteredPassword = joinPasswordInput != null ? joinPasswordInput.text : "";

        if (enteredPassword == lobby.password)
        {
            passwordDialog.SetActive(false);
            JoinLobbyDirectly(lobby);
        }
        else
        {
            if (passwordErrorText != null)
            {
                passwordErrorText.text = "Incorrect password!";
                passwordErrorText.color = Color.red;
            }
        }
    }

    private void JoinLobbyDirectly(LobbyEntry lobby)
    {
        // Update GameSession settings
        GameSession.SetRoom(lobby.roomName, lobby.mapName, lobby.maxPlayers);

        Debug.Log($"[FLOW:SETUP] LobbyBrowser joined room {lobby.roomName}. Transitioning to Lobby scene.");
        SceneManager.LoadScene(lobbySceneName);
    }
}
