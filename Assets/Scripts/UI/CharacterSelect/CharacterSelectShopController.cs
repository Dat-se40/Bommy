using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectShopController : MonoBehaviour
{
    [System.Serializable]
    public class CharacterInfo
    {
        [Header("Identity")]
        public string characterId;
        public string characterName;
        [TextArea] public string description;

        [Header("Visual")]
        public Sprite icon;
        public Sprite preview;

        [Header("Stats")]
        [Range(1, 5)] public int hp = 3;
        [Range(1, 5)] public int bomb = 1;
        [Range(1, 100)] public int speed = 60;

        [Header("Shop")]
        public bool defaultOwned;
        public int requiredLevel = 1;
        public int price;
    }

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string randomMatchSceneName = "RandomMatch";

    [Header("Player Progress UI")]
    [SerializeField] private TMP_Text goldlbl;
    [SerializeField] private TMP_Text levellbl;

    [Header("Character Data")]
    [SerializeField] private CharacterInfo[] characters;

    [Header("Character Cards")]
    [SerializeField] private CharacterCardUI[] characterCards;

    [Header("Preview UI")]
    [SerializeField] private Image characterPreview;
    [SerializeField] private TMP_Text selectedNamelbl;
    [SerializeField] private TMP_Text selectedDeslbl;
    [SerializeField] private Image hpFill;
    [SerializeField] private Image bombFill;
    [SerializeField] private Image speedFill;
    [SerializeField] private TMP_Text requirelbl;

    [Header("Buttons")]
    [SerializeField] private Button readybtn;
    [SerializeField] private TMP_Text readybtnlbl;
    [SerializeField] private Button backbtn;
    [SerializeField] private Button leftbtn;
    [SerializeField] private Button rightbtn;

    [Header("Test Default Progress")]
    [SerializeField] private int defaultGold = 850;
    [SerializeField] private int defaultLevel = 1;

    private int selectedIndex;
    private int playerGold;
    private int playerLevel;

    private const string GoldKey = "PlayerGold";
    private const string LevelKey = "PlayerLevel";
    private const string ModeKey = "CharacterSelectMode";

    private void Start()
    {
        LoadProgress();
        SetupButtons();

        if (characters != null && characters.Length > 0)
            SelectCharacter(0);
        else
            RefreshAllCards();
    }

    private void LoadProgress()
    {
        playerGold = PlayerPrefs.GetInt(GoldKey, defaultGold);
        playerLevel = PlayerPrefs.GetInt(LevelKey, defaultLevel);

        RefreshProgressUI();
    }

    private void RefreshProgressUI()
    {
        if (goldlbl != null)
            goldlbl.text = "Gold: " + playerGold;

        if (levellbl != null)
            levellbl.text = "Level: " + playerLevel;
    }

    private void SetupButtons()
    {
        if (readybtn != null)
        {
            readybtn.onClick.RemoveAllListeners();
            readybtn.onClick.AddListener(OnReadyButtonClicked);
        }

        if (backbtn != null)
        {
            backbtn.onClick.RemoveAllListeners();
            backbtn.onClick.AddListener(Back);
        }

        if (leftbtn != null)
        {
            leftbtn.onClick.RemoveAllListeners();
            leftbtn.onClick.AddListener(PreviousCharacter);
        }

        if (rightbtn != null)
        {
            rightbtn.onClick.RemoveAllListeners();
            rightbtn.onClick.AddListener(NextCharacter);
        }
    }

    private void RefreshAllCards()
    {
        if (characterCards == null)
            return;

        for (int i = 0; i < characterCards.Length; i++)
        {
            if (characterCards[i] == null)
                continue;

            if (characters == null || i >= characters.Length)
            {
                characterCards[i].gameObject.SetActive(false);
                continue;
            }

            characterCards[i].gameObject.SetActive(true);

            CharacterInfo data = characters[i];

            bool owned = IsOwned(data);
            bool levelUnlocked = IsLevelUnlocked(data);
            bool selected = i == selectedIndex;

            characterCards[i].Setup(
                this,
                i,
                data,
                owned,
                levelUnlocked,
                selected
            );
        }
    }

    public void SelectCharacter(int index)
    {
        if (characters == null || characters.Length == 0)
            return;

        if (index < 0 || index >= characters.Length)
            return;

        selectedIndex = index;

        CharacterInfo data = characters[selectedIndex];

        if (characterPreview != null)
            characterPreview.sprite = data.preview != null ? data.preview : data.icon;

        if (selectedNamelbl != null)
            selectedNamelbl.text = data.characterName;

        if (selectedDeslbl != null)
            selectedDeslbl.text = data.description;

        if (hpFill != null)
            hpFill.fillAmount = data.hp / 5f;

        if (bombFill != null)
            bombFill.fillAmount = data.bomb / 5f;

        if (speedFill != null)
            speedFill.fillAmount = data.speed / 100f;

        UpdateReadyButtonState();
        RefreshAllCards();
    }

    private void PreviousCharacter()
    {
        if (characters == null || characters.Length == 0)
            return;

        int nextIndex = selectedIndex - 1;

        if (nextIndex < 0)
            nextIndex = characters.Length - 1;

        SelectCharacter(nextIndex);
    }

    private void NextCharacter()
    {
        if (characters == null || characters.Length == 0)
            return;

        int nextIndex = selectedIndex + 1;

        if (nextIndex >= characters.Length)
            nextIndex = 0;

        SelectCharacter(nextIndex);
    }

    private void UpdateReadyButtonState()
    {
        CharacterInfo data = characters[selectedIndex];

        bool owned = IsOwned(data);
        bool levelUnlocked = IsLevelUnlocked(data);
        bool enoughGold = playerGold >= data.price;

        if (owned)
        {
            SetReadyButton("READY", true);

            if (requirelbl != null)
                requirelbl.text = "OWNED";

            return;
        }

        if (!levelUnlocked)
        {
            SetReadyButton("LOCKED", false);

            if (requirelbl != null)
                requirelbl.text = "Requires LV " + data.requiredLevel;

            return;
        }

        if (!enoughGold)
        {
            SetReadyButton("BUY " + data.price, false);

            if (requirelbl != null)
                requirelbl.text = "Need " + (data.price - playerGold) + " more gold";

            return;
        }

        SetReadyButton("BUY " + data.price, true);

        if (requirelbl != null)
            requirelbl.text = "Available to buy";
    }

    private void SetReadyButton(string text, bool interactable)
    {
        if (readybtnlbl != null)
            readybtnlbl.text = text;

        if (readybtn != null)
            readybtn.interactable = interactable;
    }

    private void OnReadyButtonClicked()
    {
        CharacterInfo data = characters[selectedIndex];

        if (IsOwned(data))
        {
            Ready();
            return;
        }

        TryBuyCharacter(data);
    }

    private void TryBuyCharacter(CharacterInfo data)
    {
        if (!IsLevelUnlocked(data))
            return;

        if (playerGold < data.price)
            return;

        playerGold -= data.price;

        PlayerPrefs.SetInt(GoldKey, playerGold);
        PlayerPrefs.SetInt(GetOwnedKey(data.characterId), 1);
        PlayerPrefs.Save();

        RefreshProgressUI();
        UpdateReadyButtonState();
        RefreshAllCards();
    }

    private void Ready()
    {
        CharacterInfo data = characters[selectedIndex];

        PlayerPrefs.SetInt("SelectedCharacterIndex", selectedIndex);
        PlayerPrefs.SetString("SelectedCharacterId", data.characterId);
        PlayerPrefs.SetString("SelectedCharacterName", data.characterName);
        PlayerPrefs.SetInt("SelectedCharacterHp", data.hp);
        PlayerPrefs.SetInt("SelectedCharacterBomb", data.bomb);
        PlayerPrefs.SetInt("SelectedCharacterSpeed", data.speed);
        PlayerPrefs.Save();

        string mode = PlayerPrefs.GetString(ModeKey, "Play");

        if (mode == "Lobby")
            SceneManager.LoadScene(lobbySceneName);
        else
            SceneManager.LoadScene(randomMatchSceneName);
    }

    private void Back()
    {
        string mode = PlayerPrefs.GetString(ModeKey, "Play");

        if (mode == "Lobby")
            SceneManager.LoadScene(lobbySceneName);
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }

    private bool IsOwned(CharacterInfo data)
    {
        if (data.defaultOwned)
            return true;

        return PlayerPrefs.GetInt(GetOwnedKey(data.characterId), 0) == 1;
    }

    private bool IsLevelUnlocked(CharacterInfo data)
    {
        return playerLevel >= data.requiredLevel;
    }

    private string GetOwnedKey(string characterId)
    {
        return "CharacterOwned_" + characterId;
    }
}
