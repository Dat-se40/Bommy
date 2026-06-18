using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Game over UI — subscribe MatchGameplayAuthority.MatchFinishedChanged (replicate mọi client).
/// Script phải nằm trên object cha; gameOverOverlay là child bị ẩn lúc Start.
/// </summary>
public class GameOverUIController : MonoBehaviour
{
    [System.Serializable]
    public class LeaderboardRowUI
    {
        public TMP_Text ranklbl;
        public TMP_Text playerNamelbl;
        public TMP_Text killslbl;
        public TMP_Text scorelbl;
        public GameObject gameObject;
    }

    [Header("Scenes")]
    [SerializeField] private string randomMatchSceneName = "RandomMatch";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Panels")]
    [SerializeField] private GameObject gameOverOverlay;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject leaderboardPanel;

    [Header("Game Over Labels")]
    [SerializeField] private TMP_Text resultlbl;
    [SerializeField] private TMP_Text summarylbl;
    [SerializeField] private TMP_Text killslbl;
    [SerializeField] private TMP_Text scorelbl;
    [SerializeField] private TMP_Text timelbl;

    [Header("Reward Labels")]
    [SerializeField] private TMP_Text goldRewardlbl;
    [SerializeField] private TMP_Text expRewardlbl;
    [SerializeField] private TMP_Text levellbl;
    [SerializeField] private TMP_Text nextLevellbl;
    [SerializeField] private TMP_Text explbl;
    [SerializeField] private Image expFill;

    [Header("Buttons")]
    [SerializeField] private Button playbtn;
    [SerializeField] private Button leaderboardbtn;
    [SerializeField] private Button mainMenubtn;
    [SerializeField] private Button backbtn;
    [SerializeField] private Button mainMenu2btn;

    [Header("Leaderboard Rows")]
    [SerializeField] private LeaderboardRowUI[] leaderboardRows;

    [Header("Match End")]
    [SerializeField] private float matchStartTime;

    bool matchEnded;

    void Awake()
    {
        matchStartTime = Time.time;
    }

    void Start()
    {
        SetupButtons();
        HideGameOver();
        StartCoroutine(BindMatchEndWhenReady());
    }

    IEnumerator BindMatchEndWhenReady()
    {
        while (MatchGameplayAuthority.Instance == null)
            yield return null;

        MatchGameplayAuthority authority = MatchGameplayAuthority.Instance;
        authority.MatchFinishedChanged += OnMatchFinishedChanged;

        if (authority.IsMatchFinished)
            OnMatchFinishedChanged(true);
    }

    void OnDestroy()
    {
        if (MatchGameplayAuthority.Instance != null)
            MatchGameplayAuthority.Instance.MatchFinishedChanged -= OnMatchFinishedChanged;
    }

    void OnMatchFinishedChanged(bool finished)
    {
        if (!finished || matchEnded)
            return;

        matchEnded = true;
        StartCoroutine(PresentMatchEndWhenReady());
    }

    IEnumerator PresentMatchEndWhenReady()
    {
        MatchGameplayAuthority authority = MatchGameplayAuthority.Instance;

        float timeout = 3f;
        while (authority != null && authority.LeaderBoardData.Count == 0 && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        yield return null;

        PresentMatchEnd();
    }

    static PlayerBoardState FindLocalBoardState()
    {
        PlayerController[] controllers = FindObjectsByType<PlayerController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < controllers.Length; i++)
        {
            if (!controllers[i].isOwner)
                continue;

            if (controllers[i].TryGetComponent(out PlayerBoardState board))
                return board;
        }

        return null;
    }

    static bool TryGetLocalLeaderBoardData(
        PlayerBoardState localBoard,
        MatchGameplayAuthority authority,
        out LeaderBoardData data
    )
    {
        data = default;

        if (authority == null || authority.LeaderBoardData.Count == 0)
            return false;

        int localSlot = localBoard != null ? localBoard.SlotIndex : -1;

        for (int i = 0; i < authority.LeaderBoardData.Count; i++)
        {
            LeaderBoardData entry = authority.LeaderBoardData[i];

            if (localSlot >= 0 && entry.slotIndex == localSlot)
            {
                data = entry;
                return true;
            }
        }

        if (localBoard == null)
            return false;

        string localName = localBoard.DisplayName;

        for (int i = 0; i < authority.LeaderBoardData.Count; i++)
        {
            LeaderBoardData entry = authority.LeaderBoardData[i];

            if (entry.name == localName)
            {
                data = entry;
                return true;
            }
        }

        return false;
    }

    void PresentMatchEnd()
    {
        MatchGameplayAuthority authority = MatchGameplayAuthority.Instance;
        PlayerBoardState localBoard = FindLocalBoardState();

        bool hasLeaderData = TryGetLocalLeaderBoardData(localBoard, authority, out LeaderBoardData localData);

        bool isWin = localBoard != null && !localBoard.IsEliminated;
        int kills = hasLeaderData ? localData.kills : (localBoard != null ? localBoard.Kills : 0);
        int score = hasLeaderData ? localData.score : (localBoard != null ? localBoard.Score : 0);
        string timeText = FormatElapsedTime(Time.time - matchStartTime);

        int goldReward = Mathf.Max(10, score / 20);
        int expReward = Mathf.Max(20, score / 10);
        PlayerAccountSnapshot progression = PlayerProgressionService.Instance?.Current;
        int level = progression?.level ?? 1;
        int currentExp = progression?.experience ?? 0;
        int needExp = 100 + level * 50;

        ShowGameOver(
            isWin,
            kills,
            score,
            timeText,
            goldReward,
            expReward,
            level,
            currentExp,
            needExp
        );

        PopulateLeaderboardFromBoardStates();
    }

    static string FormatElapsedTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = total / 60;
        int secs = total % 60;
        return minutes.ToString("00") + ":" + secs.ToString("00");
    }

    void PopulateLeaderboardFromBoardStates()
    {
        MatchGameplayAuthority authority = MatchGameplayAuthority.Instance;
        if (authority == null)
            return;

        int lbSize = authority.LeaderBoardData.Count;

        for (int i = 0; i < leaderboardRows.Length; i++)
        {
            LeaderboardRowUI row = leaderboardRows[i];

            if (row.gameObject == null)
                continue;

            if (i >= lbSize)
            {
                row.gameObject.SetActive(false);
                continue;
            }

            row.gameObject.SetActive(true);
            LeaderBoardData data = authority.LeaderBoardData[i];

            if (row.ranklbl != null)
                row.ranklbl.text = (i + 1).ToString();

            if (row.playerNamelbl != null)
                row.playerNamelbl.text = string.IsNullOrEmpty(data.name) ? "UNKNOWN" : data.name;

            if (row.killslbl != null)
                row.killslbl.text = data.kills.ToString();

            if (row.scorelbl != null)
                row.scorelbl.text = data.score.ToString();
        }
    }

    void SetupButtons()
    {
        if (playbtn != null)
        {
            playbtn.onClick.RemoveAllListeners();
            playbtn.onClick.AddListener(PlayAgain);
        }

        if (leaderboardbtn != null)
        {
            leaderboardbtn.onClick.RemoveAllListeners();
            leaderboardbtn.onClick.AddListener(OpenLeaderboard);
        }

        if (mainMenubtn != null)
        {
            mainMenubtn.onClick.RemoveAllListeners();
            mainMenubtn.onClick.AddListener(GoMainMenu);
        }

        if (backbtn != null)
        {
            backbtn.onClick.RemoveAllListeners();
            backbtn.onClick.AddListener(BackToGameOver);
        }

        if (mainMenu2btn != null)
        {
            mainMenu2btn.onClick.RemoveAllListeners();
            mainMenu2btn.onClick.AddListener(GoMainMenu);
        }
    }

    public void HideGameOver()
    {
        if (gameOverOverlay != null)
            gameOverOverlay.SetActive(false);
    }

    public void ShowDemoGameOver()
    {
        ShowGameOver(
            isWin: true,
            kills: 4,
            score: 2500,
            timeText: "02:14",
            goldReward: 120,
            expReward: 280,
            currentLevel: 5,
            currentExp: 320,
            needExp: 600
        );

        SetDemoLeaderboard();
    }

    public void ShowGameOver(
        bool isWin,
        int kills,
        int score,
        string timeText,
        int goldReward,
        int expReward,
        int currentLevel,
        int currentExp,
        int needExp
    )
    {
        if (gameOverOverlay != null)
            gameOverOverlay.SetActive(true);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (resultlbl != null)
            resultlbl.text = isWin ? "VICTORY" : "DEFEAT";

        if (summarylbl != null)
            summarylbl.text = isWin
                ? "You survived the bomb battle."
                : "You were blasted out.";

        if (killslbl != null)
            killslbl.text = kills.ToString();

        if (scorelbl != null)
            scorelbl.text = score.ToString();

        if (timelbl != null)
            timelbl.text = timeText;

        if (goldRewardlbl != null)
            goldRewardlbl.text = "+" + goldReward;

        if (expRewardlbl != null)
            expRewardlbl.text = "+" + expReward;

        ApplyExp(currentLevel, currentExp, needExp, expReward);
    }

    void ApplyExp(int currentLevel, int currentExp, int needExp, int expReward)
    {
        int newExp = currentExp + expReward;
        int newLevel = currentLevel;
        int newNeedExp = needExp;

        while (newExp >= newNeedExp)
        {
            newExp -= newNeedExp;
            newLevel++;
            newNeedExp = Mathf.RoundToInt(newNeedExp * 1.25f);
        }

        if (levellbl != null)
            levellbl.text = "LV " + newLevel;

        if (nextLevellbl != null)
            nextLevellbl.text = "LV " + (newLevel + 1);

        if (explbl != null)
            explbl.text = newExp + " / " + newNeedExp + " EXP";

        if (expFill != null)
            expFill.fillAmount = (float)newExp / newNeedExp;
    }

    void SetDemoLeaderboard()
    {
        SetLeaderboardRow(0, "🥇", "BLASTER (YOU)", 4, 2500);
        SetLeaderboardRow(1, "🥈", "MIMI", 2, 1200);
        SetLeaderboardRow(2, "🥉", "LUNA", 1, 800);
        SetLeaderboardRow(3, "#4", "POKO", 0, 300);
    }

    void SetLeaderboardRow(int index, string rank, string playerName, int kills, int score)
    {
        if (leaderboardRows == null || index < 0 || index >= leaderboardRows.Length)
            return;

        LeaderboardRowUI row = leaderboardRows[index];

        if (row.ranklbl != null)
            row.ranklbl.text = rank;

        if (row.playerNamelbl != null)
            row.playerNamelbl.text = playerName;

        if (row.killslbl != null)
            row.killslbl.text = kills.ToString();

        if (row.scorelbl != null)
            row.scorelbl.text = score.ToString();
    }

    public void OpenLeaderboard()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);
    }

    public void BackToGameOver()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(randomMatchSceneName);
    }

    public void GoMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
