using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Entry point for the game. Bootstraps AuthService and redirects to AuthGate
/// if there is no valid session.
/// </summary>
public sealed class MainMenuBootstrapper : MonoBehaviour
{
    [SerializeField] private string authGateSceneName = "AuthGate";

    private async void Awake()
    {
        AuthService auth = AuthService.GetOrCreate();

        if (auth.IsAuthenticated)
        {
            Debug.Log("[MainMenuBootstrapper] User is already authenticated. Skipping session restore.");
            return;
        }

        AuthResult result = await auth.TryRestoreSessionAsync();

        if (!result.Success)
        {
            Debug.LogFormat("Failed to restore session, switching to auth gate: {0}", result.Error);
            SceneManager.LoadScene(authGateSceneName);
        }
    }
}
