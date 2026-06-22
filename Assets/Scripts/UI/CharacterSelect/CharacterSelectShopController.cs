using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class CharacterSelectShopController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string randomMatchSceneName = "RandomMatchDemo";

    [Header("Player Progress UI")]
    [SerializeField] private TMP_Text goldlbl;

    [Header("Character Database")]
    [SerializeField] private CharacterDatabase characterDatabase;

    [Header("Character Cards")]
    [SerializeField] private Transform cardContent;
    [SerializeField] private CharacterCardUI characterCardTemplate;

    readonly List<CharacterCardUI> characterCards = new();

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

    int selectedIndex;
    int playerGold;
    bool requestInProgress;

    const string ModeKey = "CharacterSelectMode";

    void OnEnable()
    {
        PlayerProgressionService.ProgressionChanged += OnProgressionChanged;
    }

    void OnDisable()
    {
        PlayerProgressionService.ProgressionChanged -= OnProgressionChanged;
    }

    void Start()
    {
        if (characterDatabase != null)
            MatchSessionBroker.SetCharacterCatalog(characterDatabase);

        LoadProgress();
        SetupButtons();
        BuildCharacterCards();
        SelectInitialCharacter();
    }

    void LoadProgress()
    {
        PlayerAccountSnapshot progression = PlayerProgressionService.Instance?.Current;
        playerGold = progression?.gold ?? 0;
        RefreshProgressUI();
    }

    void RefreshProgressUI()
    {
        if (goldlbl != null)
            goldlbl.text = "Gold: " + playerGold;

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

        int savedCharacterId = PlayerProgressionService.Instance?.Current?.selectedCharacterId ?? 1;
        int savedIndex = characterDatabase.GetIndexById(savedCharacterId);

        SelectCharacter(savedIndex >= 0 ? savedIndex : 0);
    }

    void RefreshAllCards()
    {
        if (characterCards == null)
            return;

        for (int i = 0; i < characterCards.Count; i++)
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
            bool selected = i == selectedIndex;

            characterCards[i].Setup(this, i, data, owned, selected);
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
        bool enoughGold = playerGold >= data.Price;

        if (owned)
        {
            SetReadyButton("READY", true);

            if (requirelbl != null)
                requirelbl.text = "OWNED";

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

    async void OnReadyButtonClicked()
    {
        if (characterDatabase == null)
            return;
        CharacterDefinition data = characterDatabase.GetByIndex(selectedIndex);

        if (data == null)
            return;

        if (IsOwned(data))
        {
            await ReadyAsync(data);
            return;
        }

        await TryBuyCharacterAsync(data);
    }

    async System.Threading.Tasks.Task TryBuyCharacterAsync(CharacterDefinition data)
    {
        if (requestInProgress || playerGold < data.Price)
            return;

        requestInProgress = true;
        SetReadyButton("BUYING...", false);

        try
        {
            await PlayerProgressionService.EnsureExists().PurchaseCharacterAsync(data.CharacterId);
        }
        catch (System.Exception exception)
        {
            if (requirelbl != null)
                requirelbl.text = exception.Message;
        }
        finally
        {
            requestInProgress = false;
            LoadProgress();
            UpdateReadyButtonState();
            RefreshAllCards();
        }
    }

    async System.Threading.Tasks.Task ReadyAsync(CharacterDefinition data)
    {
        if (requestInProgress)
            return;

        requestInProgress = true;
        SetReadyButton("SAVING...", false);

        try
        {
            await PlayerProgressionService.EnsureExists().SelectCharacterAsync(data.CharacterId);
        }
        catch (System.Exception exception)
        {
            if (requirelbl != null)
                requirelbl.text = exception.Message;

            requestInProgress = false;
            UpdateReadyButtonState();
            return;
        }

        string displayName = NakamaConnectionManager.EnsureExists().DisplayName;

        PlayerMatchProfile profile = PlayerMatchProfile.FromDefinition(
            data,
            selectedIndex,
            slotIndex: 0,
            isLocal: true,
            displayNameOverride: displayName
        );

        MatchSessionBroker.CommitLocalSelection(profile);
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
        if (data == null)
            return false;

        if (data.DefaultOwned)
            return true;

        return PlayerProgressionService.Instance != null &&
            PlayerProgressionService.Instance.OwnsCharacter(data.CharacterId);
    }



    void OnProgressionChanged()
    {
        LoadProgress();
        UpdateReadyButtonState();
        RefreshAllCards();
    }

    /// <summary>
    /// Tạo card nhân vật từ CharacterDatabase bằng một template duy nhất.
    /// </summary>
    void BuildCharacterCards()
    {
        characterCards.Clear();

        if (cardContent == null || characterCardTemplate == null)
        {
            Debug.LogWarning("[FLOW:SETUP] Character card template/content is missing.", this);
            return;
        }

        if (characterDatabase == null)
            return;

        characterCardTemplate.gameObject.SetActive(false);

        for (int i = 0; i < characterDatabase.Count; i++)
        {
            CharacterCardUI card = Instantiate(characterCardTemplate, cardContent);
            card.name = "CharacterCard_" + i;
            card.gameObject.SetActive(true);

            characterCards.Add(card);
        }
    }

}
