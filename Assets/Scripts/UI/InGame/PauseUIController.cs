using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseUIController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Panels")]
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button pausebtn;
    [SerializeField] private Button resumebtn;
    [SerializeField] private Button settingsbtn;
    [SerializeField] private Button quitbtn;

    [Header("Labels")]
    [SerializeField] private TMP_Text centerMessagelbl;

    private bool isPaused;

    private void Start()
    {
        SetupButtons();
        ResumeGame();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    private void SetupButtons()
    {
        if (pausebtn != null)
        {
            pausebtn.onClick.RemoveAllListeners();
            pausebtn.onClick.AddListener(PauseGame);
        }

        if (resumebtn != null)
        {
            resumebtn.onClick.RemoveAllListeners();
            resumebtn.onClick.AddListener(ResumeGame);
        }

        if (settingsbtn != null)
        {
            settingsbtn.onClick.RemoveAllListeners();
            settingsbtn.onClick.AddListener(OpenSettings);
        }

        if (quitbtn != null)
        {
            quitbtn.onClick.RemoveAllListeners();
            quitbtn.onClick.AddListener(QuitToMenu);
        }
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pauseOverlay != null)
            pauseOverlay.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (centerMessagelbl != null)
            centerMessagelbl.text = "PAUSED";
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (centerMessagelbl != null)
            centerMessagelbl.text = "";
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
        else
            Debug.Log("SettingsPanel is not assigned.");
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
