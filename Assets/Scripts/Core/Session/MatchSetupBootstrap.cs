using UnityEngine;

/// <summary>
/// Module SETUP — gắn ở CharacterSelect / Lobby entry.
/// Khởi tạo catalog + fetch account trước khi UI shop chạy.
/// </summary>
public class MatchSetupBootstrap : MonoBehaviour
{
    [SerializeField] private CharacterDatabase characterDatabase;

    void Awake()
    {
        if (characterDatabase != null)
            MatchSessionBroker.SetCharacterCatalog(characterDatabase);

        if (DedicatedServerBootstrap.IsDedicatedServerRuntime)
            return;

        MatchSessionBroker.LoadLocalFromProgression(characterDatabase);
    }
}
