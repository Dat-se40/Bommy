using UnityEngine;

public sealed class ClientAuthBootstrap : MonoBehaviour
{
    [SerializeField] private SteamAuthProvider steamAuthProvider;
    [SerializeField] private PlayerProfileApiClient apiClient;
    [SerializeField] private bool authenticateOnStart = true;

    void Start()
    {
        if (!authenticateOnStart || RuntimeMode.IsDedicatedServer)
            return;

        if (apiClient == null)
            apiClient = FindAnyObjectByType<PlayerProfileApiClient>();

        if (steamAuthProvider == null)
            steamAuthProvider = FindAnyObjectByType<SteamAuthProvider>();

        if (apiClient == null)
            return;

        if (steamAuthProvider == null)
        {
            apiClient.FetchLocalAccount(MatchSessionBroker.ApplyAccountSnapshot);
            return;
        }

        steamAuthProvider.RequestTicket((success, ticket) =>
        {
            if (success)
            {
                apiClient.AuthenticateWithSteamTicket(ticket, (_, account) =>
                {
                    MatchSessionBroker.ApplyAccountSnapshot(account);
                });
                return;
            }

            apiClient.FetchLocalAccount(MatchSessionBroker.ApplyAccountSnapshot);
        });
    }
}
