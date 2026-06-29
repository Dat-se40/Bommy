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
        if (DedicatedServerBootstrap.IsDedicatedServerRuntime)
            return;

        AuthService auth = AuthService.GetOrCreate();

        if (auth.IsAuthenticated)
        {
            Debug.Log("[MainMenuBootstrapper] User is already authenticated. Skipping session restore.");
            CheckSteamLaunchArguments();
            return;
        }

        AuthResult result = await auth.TryRestoreSessionAsync();

        if (!result.Success)
        {
            Debug.LogFormat("Failed to restore session, switching to auth gate: {0}", result.Error);
            SceneManager.LoadScene(authGateSceneName);
        }
        else
        {
            CheckSteamLaunchArguments();
        }
    }

    private void CheckSteamLaunchArguments()
    {
        if (SteamService.Instance == null)
            return;

        string launchConnect = SteamService.Instance.GetLaunchCommandLineConnectString();
        if (!string.IsNullOrEmpty(launchConnect) && launchConnect.StartsWith("nakama_lobby:"))
        {
            string code = launchConnect.Substring("nakama_lobby:".Length);
            Debug.LogFormat("[MainMenuBootstrapper] Found Steam launch connect code: {0}. Attempting auto-join.", code);
            LobbyManager.EnsureExists().RequestJoinRoomByCode(new JoinRoomByCodeRequest { roomCode = code, password = code });
        }
    }
}
