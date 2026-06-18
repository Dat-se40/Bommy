using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectController : MonoBehaviour
{
    [System.Serializable]
    public class CharacterInfo
    {
        public string characterName;
        [TextArea] public string description;
        public Sprite previewSprite;
        public int hp;
        public int bomb;
        public int speed;
    }

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string randomMatchSceneName = "RandomMatch";

    [Header("Characters")]
    [SerializeField] private CharacterInfo[] characters;

    [Header("Character Buttons")]
    [SerializeField] private Button[] characterButtons;

    [Header("Preview UI")]
    [SerializeField] private Image characterPreview;
    [SerializeField] private TMP_Text characterName;
    [SerializeField] private TMP_Text characterDes;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text bombText;
    [SerializeField] private TMP_Text speedText;

    [Header("Navigation Buttons")]
    [SerializeField] private Button readybtn;
    [SerializeField] private Button backbtn;
    [SerializeField] private Button leftbtn;
    [SerializeField] private Button rightbtn;


    private int selectedIndex;

    private void Start()
    {
        SetupButtons();
        SelectCharacter(0);
    }

    private void SetupButtons()
    {
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;

            if (characterButtons[i] != null)
            {
                characterButtons[i].onClick.RemoveAllListeners();
                characterButtons[i].onClick.AddListener(() => SelectCharacter(index));
            }
        }

        if (readybtn != null)
        {
            readybtn.onClick.RemoveAllListeners();
            readybtn.onClick.AddListener(Ready);
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

    public void SelectCharacter(int index)
    {
        if (characters == null || characters.Length == 0)
            return;

        if (index < 0 || index >= characters.Length)
            return;

        selectedIndex = index;

        CharacterInfo data = characters[selectedIndex];

        if (characterPreview != null)
            characterPreview.sprite = data.previewSprite;

        if (characterName != null)
            characterName.text = data.characterName;

        if (characterDes != null)
            characterDes.text = data.description;

        if (hpText != null)
            hpText.text = "HP: " + data.hp;

        if (bombText != null)
            bombText.text = "BOMB: " + data.bomb;

        if (speedText != null)
            speedText.text = "SPEED: " + data.speed;

        SaveSelectedCharacter(data);
    }

    public void PreviousCharacter()
    {
        int nextIndex = selectedIndex - 1;

        if (nextIndex < 0)
            nextIndex = characters.Length - 1;

        SelectCharacter(nextIndex);
    }

    public void NextCharacter()
    {
        int nextIndex = selectedIndex + 1;

        if (nextIndex >= characters.Length)
            nextIndex = 0;

        SelectCharacter(nextIndex);
    }

    public void Ready()
    {
        if (characters == null || characters.Length == 0)
            return;

        CharacterInfo data = characters[selectedIndex];
        SaveSelectedCharacter(data);

        string mode = PlayerPrefs.GetString("CharacterSelectMode", "Play");

        if (mode == "Lobby")
        {
            SceneManager.LoadScene(lobbySceneName);
        }
        else
        {
            SceneManager.LoadScene(randomMatchSceneName);
        }
    }

    public void Back()
    {
        string mode = PlayerPrefs.GetString("CharacterSelectMode", "Play");

        if (mode == "Lobby")
        {
            SceneManager.LoadScene(lobbySceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void SaveSelectedCharacter(CharacterInfo data)
    {
        GameSession.SetSelectedCharacter(
            selectedIndex,
            data.characterName,
            data.hp,
            data.bomb,
            data.speed
        );
    }
}
