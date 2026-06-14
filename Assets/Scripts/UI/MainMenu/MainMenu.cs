using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string characterSelectSceneName = "CharacterSelect";
    [SerializeField] private string lobbySceneName = "LobbyBrowser";

    [Header("Settings")]
    [SerializeField] private GameObject settingsPanel;

    public void Play()
    {
        PlayerPrefs.SetString("CharacterSelectMode", "Play");
        PlayerPrefs.Save();

        SceneManager.LoadScene(characterSelectSceneName);
    }

    public void Multiplayer()
    {
        SceneManager.LoadScene(lobbySceneName);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void Quit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
