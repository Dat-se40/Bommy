using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectShopController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string randomMatchSceneName = "RandomMatchDemo";

    [Header("Player Progress UI")]
    [SerializeField] private TMP_Text goldlbl;
    [SerializeField] private TMP_Text levellbl;

    [Header("Character Database")]
    [SerializeField] private CharacterDatabase characterDatabase;

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

    int selectedIndex;
    int playerGold;
    int playerLevel;

    const string GoldKey = "PlayerGold";
    const string LevelKey = "PlayerLevel";
    const string ModeKey = "CharacterSelectMode";

    void Start()
    {
        if (characterDatabase != null)
            MatchSessionBroker.SetCharacterCatalog(characterDatabase);

        LoadProgress();
        SetupButtons();
        SelectInitialCharacter();
    }

    void LoadProgress()
    {
        playerGold = PlayerPrefs.GetInt(GoldKey, defaultGold);
        playerLevel = PlayerPrefs.GetInt(LevelKey, defaultLevel);
        RefreshProgressUI();
    }

    void RefreshProgressUI()
    {
        if (goldlbl != null)
            goldlbl.text = "Gold: " + playerGold;

        if (levellbl != null)
            levellbl.text = "Level: " + playerLevel;
    }

    void SetupButtons()
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

    void SelectInitialCharacter()
    {
        if (characterDatabase == null || characterDatabase.Count == 0)
        {
            RefreshAllCards();
            return;
        }

        int savedCharacterId = PlayerPrefs.GetInt("SelectedCharacterId", -1);
        int savedIndex = characterDatabase.GetIndexById(savedCharacterId);

        SelectCharacter(savedIndex >= 0 ? savedIndex : 0);
    }

    void RefreshAllCards()
    {
        if (characterCards == null)
            return;

        for (int i = 0; i < characterCards.Length; i++)
        {
            if (characterCards[i] == null)
                continue;

            CharacterDefinition data = characterDatabase != null
                ? characterDatabase.GetByIndex(i)
                : null;

            if (data == null)
            {
                characterCards[i].gameObject.SetActive(false);
                continue;
            }

            characterCards[i].gameObject.SetActive(true);

            bool owned = IsOwned(data);
            bool levelUnlocked = IsLevelUnlocked(data);
            bool selected = i == selectedIndex;

            characterCards[i].Setup(this, i, data, owned, levelUnlocked, selected);
        }
    }

    public void SelectCharacter(int index)
    {
        if (characterDatabase == null || characterDatabase.Count == 0)
            return;

        CharacterDefinition data = characterDatabase.GetByIndex(index);

        if (data == null)
            return;

        selectedIndex = index;

        if (characterPreview != null)
            characterPreview.sprite = data.Preview;

        if (selectedNamelbl != null)
            selectedNamelbl.text = data.CharacterName;

        if (selectedDeslbl != null)
            selectedDeslbl.text = data.Description;

        if (hpFill != null)
            hpFill.fillAmount = data.Hp / 5f;

        if (bombFill != null)
            bombFill.fillAmount = data.Bomb / 5f;

        if (speedFill != null)
            speedFill.fillAmount = data.Speed / 100f;

        UpdateReadyButtonState();
        RefreshAllCards();
    }

    void PreviousCharacter()
    {
        if (characterDatabase == null || characterDatabase.Count == 0)
            return;

        int nextIndex = selectedIndex - 1;

        if (nextIndex < 0)
            nextIndex = characterDatabase.Count - 1;

        SelectCharacter(nextIndex);
    }

    void NextCharacter()
    {
        if (characterDatabase == null || characterDatabase.Count == 0)
            return;

        int nextIndex = selectedIndex + 1;

        if (nextIndex >= characterDatabase.Count)
            nextIndex = 0;

        SelectCharacter(nextIndex);
    }

    void UpdateReadyButtonState()
    {
        if (characterDatabase == null)
            return;

        CharacterDefinition data = characterDatabase.GetByIndex(selectedIndex);

        if (data == null)
            return;

        bool owned = IsOwned(data);
        bool levelUnlocked = IsLevelUnlocked(data);
        bool enoughGold = playerGold >= data.Price;

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
                requirelbl.text = "Requires LV " + data.RequiredLevel;

            return;
        }

        if (!enoughGold)
        {
            SetReadyButton("BUY " + data.Price, false);

            if (requirelbl != null)
                requirelbl.text = "Need " + (data.Price - playerGold) + " more gold";

            return;
        }

        SetReadyButton("BUY " + data.Price, true);

        if (requirelbl != null)
            requirelbl.text = "Available to buy";
    }


    void SetReadyButton(string text, bool interactable)
    {
        if (readybtnlbl != null)
            readybtnlbl.text = text;

        if (readybtn != null)
            readybtn.interactable = interactable;
    }

    void OnReadyButtonClicked()
    {
        if (characterDatabase == null)
            return;
        CharacterDefinition data = characterDatabase.GetByIndex(selectedIndex);

        if (data == null)
            return;

        if (IsOwned(data))
        {
            Ready(data);
            return;
        }

        TryBuyCharacter(data);
    }

    void TryBuyCharacter(CharacterDefinition data)
    {
        if (!IsLevelUnlocked(data))
            return;

        if (playerGold < data.Price)
            return;

        playerGold -= data.Price;

        PlayerPrefs.SetInt(GoldKey, playerGold);
        PlayerPrefs.SetInt(MatchSessionBroker.GetOwnedKey(data.CharacterId), 1);
        PlayerPrefs.Save();

        RefreshProgressUI();
        UpdateReadyButtonState();
        RefreshAllCards();
    }

    void Ready(CharacterDefinition data)
    {
        string displayName = PlayerPrefs.GetString(
            "PlayerDisplayName",
            data.CharacterName
        );

        PlayerMatchProfile profile = PlayerMatchProfile.FromDefinition(
            data,
            selectedIndex,
            slotIndex: 0,
            isLocal: true,
            displayNameOverride: displayName
        );

        MatchSessionBroker.CommitLocalSelection(profile);

        // Bridge cho code cũ và fallback khi scene sau chưa lấy được profile từ broker.
        PlayerPrefs.SetInt("SelectedCharacterIndex", selectedIndex);
        PlayerPrefs.SetInt("SelectedCharacterId", data.CharacterId);
        PlayerPrefs.SetString("SelectedCharacterName", data.CharacterName);
        PlayerPrefs.SetString("SelectedPlayerDisplayName", displayName);
        PlayerPrefs.SetInt("SelectedCharacterHp", data.Hp);
        PlayerPrefs.SetInt("SelectedCharacterBomb", data.Bomb);
        PlayerPrefs.SetInt("SelectedCharacterSpeed", data.Speed);
        PlayerPrefs.Save();

        // TODO[NETWORK] Sau này gửi characterId + displayName lên lobby/server.
        string mode = PlayerPrefs.GetString(ModeKey, "Play");

        if (mode == "Lobby")
            SceneManager.LoadScene(lobbySceneName);
        else
            SceneManager.LoadScene(randomMatchSceneName);
    }


    void Back()
    {
        string mode = PlayerPrefs.GetString(ModeKey, "Play");

        if (mode == "Lobby")
            SceneManager.LoadScene(lobbySceneName);
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }

    bool IsOwned(CharacterDefinition data)
    {
        if (data.DefaultOwned)
            return true;

        return PlayerPrefs.GetInt(MatchSessionBroker.GetOwnedKey(data.CharacterId), 0) == 1;
    }

    bool IsLevelUnlocked(CharacterDefinition data)
    {
        return playerLevel >= data.RequiredLevel;
    }
}
