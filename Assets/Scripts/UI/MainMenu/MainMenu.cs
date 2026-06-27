using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string characterSelectSceneName = "CharacterSelect";
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string authGateSceneName = "AuthGate";

    [Header("Settings")]
    [SerializeField] private GameObject guideOverlay;

    private void Start()
    {
        AuthService auth = AuthService.GetOrCreate();
        if (!auth.IsAuthenticated)
        {
            SceneManager.LoadScene(authGateSceneName);
            return;
        }

        if (guideOverlay != null)
            guideOverlay.SetActive(false);
    }

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

    public void OpenGuide()
    {
        if (guideOverlay != null)
        {
            guideOverlay.SetActive(true);
            guideOverlay.transform.SetAsLastSibling();
        }
    }

    public void CloseGuide()
    {
        if (guideOverlay != null)
            guideOverlay.SetActive(false);
    }


    public void Quit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
