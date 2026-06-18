using UnityEngine;

/// <summary>
/// Module SETUP — gắn ở CharacterSelect / Lobby entry.
/// Khởi tạo catalog + fetch account trước khi UI shop chạy.
/// </summary>
public class MatchSetupBootstrap : MonoBehaviour
{
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private PlayerProfileApiClient apiClient;
    void Awake()
    {
        if (characterDatabase != null)
            MatchSessionBroker.SetCharacterCatalog(characterDatabase);

        if (apiClient != null)
        {
            apiClient.FetchLocalAccount(OnAccountLoaded);
        }
        else
            MatchSessionBroker.LoadLocalFromProgression(characterDatabase);
    }

    void OnAccountLoaded(PlayerAccountSnapshot account)
    {
        MatchSessionBroker.LoadLocalFromProgression(characterDatabase);
    }
}
