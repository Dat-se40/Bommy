using UnityEngine;

/// <summary>
/// Module SETUP — gắn ở CharacterSelect / Lobby entry.
/// Khởi tạo catalog + fetch account trước khi UI shop chạy.
/// </summary>
public class MatchSetupBootstrap : MonoBehaviour
{
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private PlayerProfileApiClient apiClient;
    [SerializeField] private SamplePlayerDataSheet offlineSample;
    [SerializeField] private bool seedSampleOnStart = true;

    void Awake()
    {
        if (characterDatabase != null)
            MatchSessionBroker.SetCharacterCatalog(characterDatabase);

        if (apiClient != null)
        {
            apiClient.FetchLocalAccount(OnAccountLoaded);
            return;
        }

        BootstrapFromOffline();
    }

    void OnAccountLoaded(PlayerAccountSnapshot account)
    {
        MatchSessionBroker.ApplyAccountSnapshot(account);
        MatchSessionBroker.LoadLocalFromPlayerPrefs(characterDatabase);
        BootstrapFromOffline();
    }

    void BootstrapFromOffline()
    {
        if (seedSampleOnStart && offlineSample != null)
            MatchSessionBroker.SeedSampleLocalPlayer(offlineSample);
        else
            MatchSessionBroker.LoadLocalFromPlayerPrefs(characterDatabase);
    }
}
