using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUIController : MonoBehaviour
{
    [System.Serializable]
    public class LeaderboardRowUI
    {
        public TMP_Text ranklbl;
        public TMP_Text playerNamelbl;
        public TMP_Text killslbl;
        public TMP_Text scorelbl;
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

    private void Start()
    {
        SetupButtons();
        HideGameOver();
    }

    private void SetupButtons()
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

    private void ApplyExp(int currentLevel, int currentExp, int needExp, int expReward)
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

    private void SetDemoLeaderboard()
    {
        SetLeaderboardRow(0, "🥇", "BLASTER (YOU)", 4, 2500);
        SetLeaderboardRow(1, "🥈", "MIMI", 2, 1200);
        SetLeaderboardRow(2, "🥉", "LUNA", 1, 800);
        SetLeaderboardRow(3, "#4", "POKO", 0, 300);
    }

    private void SetLeaderboardRow(int index, string rank, string playerName, int kills, int score)
    {
        if (index < 0 || index >= leaderboardRows.Length)
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
