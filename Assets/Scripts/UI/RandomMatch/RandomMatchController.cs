using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RandomMatchController : MonoBehaviour
{
    [System.Serializable]
    public class PlayerSlotUI
    {
        public Image avatar;
        public TMP_Text namelbl;
        public TMP_Text statelbl;
        public Image background;
    }

    [System.Serializable]
    public class FakePlayer
    {
        public string playerName;
        public Sprite avatarSprite;
        public string stateText = "Ready";
    }

    [Header("Scenes")]
    [SerializeField] private string characterSelectSceneName = "CharacterSelect";
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("UI")]
    [SerializeField] private TMP_Text roomIdlbl;
    [SerializeField] private TMP_Text statuslbl;
    [SerializeField] private PlayerSlotUI[] playerSlots;
    [SerializeField] private Button cancelbtn;
    [SerializeField] private Button startbtn;

    [Header("Demo Data")]
    [SerializeField] private Sprite defaultSearchingAvatar;
    [SerializeField] private Sprite selectedCharacterAvatar;
    [SerializeField] private FakePlayer[] fakePlayers;

    [Header("Timing")]
    [SerializeField] private float findDelay = 1.2f;

    private Coroutine matchRoutine;
    private int joinedPlayers = 1;

    private void Start()
    {
        SetupButtons();
        SetupRoom();
        SetupInitialSlots();

        matchRoutine = StartCoroutine(FakeMatchmakingRoutine());
    }

    private void SetupButtons()
    {
        if (cancelbtn != null)
        {
            cancelbtn.onClick.RemoveAllListeners();
            cancelbtn.onClick.AddListener(CancelMatch);
        }

        if (startbtn != null)
        {
            startbtn.onClick.RemoveAllListeners();
            startbtn.onClick.AddListener(StartGame);
            startbtn.interactable = false;
        }
    }

    private void SetupRoom()
    {
        string roomId = "BM-" + Random.Range(1000, 9999);

        if (roomIdlbl != null)
            roomIdlbl.text = "Room: " + roomId;

        if (statuslbl != null)
            statuslbl.text = "Searching for players... 1/4";
    }

    private void SetupInitialSlots()
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (i == 0)
                SetupLocalPlayerSlot(playerSlots[i]);
            else
                SetupSearchingSlot(playerSlots[i]);
        }
    }

    private void SetupLocalPlayerSlot(PlayerSlotUI slot)
    {
        string characterName = NakamaConnectionManager.EnsureExists().DisplayName;

        if (slot.avatar != null)
        {
            if (selectedCharacterAvatar != null)
                slot.avatar.sprite = selectedCharacterAvatar;

            slot.avatar.enabled = selectedCharacterAvatar != null;
        }

        if (slot.namelbl != null)
            slot.namelbl.text = characterName;

        if (slot.statelbl != null)
            slot.statelbl.text = "Ready";

        if (slot.background != null)
            slot.background.color = HexToColor("#FFF1C9");
    }

    private void SetupSearchingSlot(PlayerSlotUI slot)
    {
        if (slot.avatar != null)
        {
            if (defaultSearchingAvatar != null)
            {
                slot.avatar.sprite = defaultSearchingAvatar;
                slot.avatar.enabled = true;
            }
            else
            {
                slot.avatar.enabled = false;
            }
        }

        if (slot.namelbl != null)
            slot.namelbl.text = "Searching...";

        if (slot.statelbl != null)
            slot.statelbl.text = "Waiting";

        if (slot.background != null)
            slot.background.color = HexToColor("#FFE6BC");
    }

    private IEnumerator FakeMatchmakingRoutine()
    {
        for (int i = 1; i < playerSlots.Length; i++)
        {
            yield return new WaitForSeconds(findDelay);

            int fakeIndex = i - 1;

            if (fakeIndex < fakePlayers.Length)
            {
                FillPlayerSlot(playerSlots[i], fakePlayers[fakeIndex]);
            }
            else
            {
                FillPlayerSlot(playerSlots[i], new FakePlayer
                {
                    playerName = "Player " + (i + 1),
                    avatarSprite = null,
                    stateText = "Ready"
                });
            }

            joinedPlayers++;

            if (statuslbl != null)
                statuslbl.text = "Searching for players... " + joinedPlayers + "/4";
        }

        if (statuslbl != null)
            statuslbl.text = "Match found! 4/4 players ready.";

        if (startbtn != null)
            startbtn.interactable = true;
    }

    private void FillPlayerSlot(PlayerSlotUI slot, FakePlayer player)
    {
        if (slot.avatar != null)
        {
            if (player.avatarSprite != null)
            {
                slot.avatar.sprite = player.avatarSprite;
                slot.avatar.enabled = true;
            }
            else
            {
                slot.avatar.enabled = false;
            }
        }

        if (slot.namelbl != null)
            slot.namelbl.text = player.playerName;

        if (slot.statelbl != null)
            slot.statelbl.text = player.stateText;

        if (slot.background != null)
            slot.background.color = HexToColor("#FFF1C9");
    }

    public void CancelMatch()
    {
        if (matchRoutine != null)
            StopCoroutine(matchRoutine);

        SceneManager.LoadScene(characterSelectSceneName);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private Color HexToColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}
