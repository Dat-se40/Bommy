using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Module REST API — fetch + validate tài khoản từ BE.
/// </summary>
public class PlayerProfileApiClient : MonoBehaviour
{
    [Header("REST")]
    [SerializeField] private string baseUrl = "http://localhost:8080";

    [Header("Offline Fallback")]
    [SerializeField] private SamplePlayerDataSheet offlineSample;
    [SerializeField] private bool allowOfflineFallback = true;

    string accessToken;

    public bool HasAccessToken => !string.IsNullOrWhiteSpace(accessToken);

    public void FetchLocalAccount(Action<PlayerAccountSnapshot> onCompleted)
    {
        if (!HasAccessToken)
        {
            onCompleted?.Invoke(GetOfflineAccount());
            return;
        }

        StartCoroutine(GetJson(
            "/v1/players/me",
            response => onCompleted?.Invoke(ParseOrFallback<PlayerAccountSnapshot>(response, GetOfflineAccount())),
            () => onCompleted?.Invoke(GetOfflineAccount())
        ));
    }

    public void AuthenticateWithSteamTicket(
        string steamTicket,
        Action<bool, PlayerAccountSnapshot> onCompleted
    )
    {
        if (string.IsNullOrWhiteSpace(steamTicket))
        {
            FlowGuard.Error(FlowGuard.TagRestApi, "Steam auth ticket is empty.", this);
            onCompleted?.Invoke(false, GetOfflineAccount());
            return;
        }

        AuthSteamRequest request = new AuthSteamRequest { steamTicket = steamTicket };

        StartCoroutine(PostJson(
            "/v1/auth/steam",
            request,
            response =>
            {
                AuthSteamResponse auth = JsonUtility.FromJson<AuthSteamResponse>(response);
                if (auth == null || string.IsNullOrWhiteSpace(auth.accessToken))
                {
                    FlowGuard.Error(FlowGuard.TagRestApi, "Steam auth response did not include an access token.", this);
                    onCompleted?.Invoke(false, GetOfflineAccount());
                    return;
                }

                accessToken = auth.accessToken;
                PlayerAccountSnapshot account = auth.account ?? GetOfflineAccount();
                MatchSessionBroker.ApplyAccountSnapshot(account);
                onCompleted?.Invoke(true, account);
            },
            () => onCompleted?.Invoke(false, GetOfflineAccount())
        ));
    }

    public void PurchaseCharacter(
        int characterId,
        Action<bool, PlayerAccountSnapshot> onCompleted
    )
    {
        if (!HasAccessToken)
        {
            onCompleted?.Invoke(TryPurchaseOffline(characterId), GetOfflineAccount());
            return;
        }

        PurchaseCharacterRequest request = new PurchaseCharacterRequest
        {
            characterId = characterId
        };

        StartCoroutine(PostJson(
            "/v1/shop/purchase",
            request,
            response =>
            {
                PurchaseCharacterResponse result =
                    JsonUtility.FromJson<PurchaseCharacterResponse>(response);

                if (result == null)
                {
                    onCompleted?.Invoke(false, GetOfflineAccount());
                    return;
                }

                if (result.account != null)
                    MatchSessionBroker.ApplyAccountSnapshot(result.account);

                onCompleted?.Invoke(result.success, result.account ?? GetOfflineAccount());
            },
            () => onCompleted?.Invoke(false, GetOfflineAccount())
        ));
    }

    public void CreateMatch(
        string roomName,
        string mapName,
        int maxPlayers,
        PlayerMatchProfile localProfile,
        Action<bool, MatchServerAllocation> onCompleted
    )
    {
        if (!HasAccessToken)
        {
            MatchServerAllocation local = new MatchServerAllocation
            {
                matchId = string.IsNullOrWhiteSpace(roomName) ? "local-match" : roomName,
                host = "127.0.0.1",
                port = RuntimeMode.Port,
                matchToken = "offline",
                edgegapDeploymentId = "local"
            };

            onCompleted?.Invoke(true, local);
            return;
        }

        CreateMatchRequest request = new CreateMatchRequest
        {
            roomName = roomName,
            mapName = mapName,
            maxPlayers = maxPlayers,
            characterId = localProfile.characterId,
            displayName = localProfile.displayName
        };

        StartCoroutine(PostJson(
            "/v1/matches/create",
            request,
            response =>
            {
                CreateMatchResponse result = JsonUtility.FromJson<CreateMatchResponse>(response);
                if (result == null)
                {
                    onCompleted?.Invoke(false, null);
                    return;
                }

                onCompleted?.Invoke(result.success, result.allocation);
            },
            () => onCompleted?.Invoke(false, null)
        ));
    }

    public Task<MatchJoinValidationResult> ValidateMatchJoinAsync(MatchJoinPayload payload)
    {
        TaskCompletionSource<MatchJoinValidationResult> completion = new();

        if (allowOfflineFallback && payload.matchToken == "offline")
        {
            completion.SetResult(MatchJoinValidationResult.Offline(payload));
            return completion.Task;
        }

        if (string.IsNullOrWhiteSpace(payload.matchId) || string.IsNullOrWhiteSpace(payload.matchToken))
        {
            completion.SetResult(new MatchJoinValidationResult
            {
                success = false,
                error = "missing match credentials"
            });
            return completion.Task;
        }

        StartCoroutine(PostJson(
            $"/v1/matches/{UnityWebRequest.EscapeURL(payload.matchId)}/validate-player",
            payload,
            response =>
            {
                MatchJoinValidationResult result =
                    JsonUtility.FromJson<MatchJoinValidationResult>(response);

                completion.SetResult(result ?? new MatchJoinValidationResult
                {
                    success = false,
                    error = "empty validation response"
                });
            },
            () => completion.SetResult(new MatchJoinValidationResult
            {
                success = false,
                error = "backend validation failed"
            })
        ));

        return completion.Task;
    }

    public void RegisterServerReady(Action<bool> onCompleted = null)
    {
        if (string.IsNullOrWhiteSpace(RuntimeMode.MatchId))
        {
            onCompleted?.Invoke(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(RuntimeMode.BackendUrl))
        {
            onCompleted?.Invoke(true);
            return;
        }

        ServerReadyRequest request = new ServerReadyRequest
        {
            edgegapDeploymentId = RuntimeMode.EdgegapDeploymentId,
            port = RuntimeMode.Port
        };

        StartCoroutine(PostJson(
            $"/v1/matches/{UnityWebRequest.EscapeURL(RuntimeMode.MatchId)}/server-ready",
            request,
            _ => onCompleted?.Invoke(true),
            () => onCompleted?.Invoke(false)
        ));
    }

    PlayerAccountSnapshot GetOfflineAccount()
    {
        if (!allowOfflineFallback)
            return new PlayerAccountSnapshot();

        PlayerAccountSnapshot snapshot = offlineSample != null
            ? offlineSample.account
            : new PlayerAccountSnapshot();

        return snapshot;
    }

    bool TryPurchaseOffline(int characterId)
    {
        if (!allowOfflineFallback)
            return false;

        PlayerAccountSnapshot account = GetOfflineAccount();
        int gold = PlayerPrefs.GetInt("PlayerGold", account.gold);

        CharacterDefinition definition = MatchSessionBroker.CharacterCatalog != null
            ? MatchSessionBroker.CharacterCatalog.GetById(characterId)
            : null;

        if (definition == null || definition.DefaultOwned)
            return true;

        if (PlayerPrefs.GetInt(MatchSessionBroker.GetOwnedKey(characterId), 0) == 1)
            return true;

        if (gold < definition.Price)
            return false;

        gold -= definition.Price;
        PlayerPrefs.SetInt("PlayerGold", gold);
        PlayerPrefs.SetInt(MatchSessionBroker.GetOwnedKey(characterId), 1);
        PlayerPrefs.Save();

        account.gold = gold;
        return true;
    }

    IEnumerator GetJson(string path, Action<string> onSuccess, Action onFailure)
    {
        using UnityWebRequest request = UnityWebRequest.Get(BuildUrl(path));
        ApplyAuthHeader(request);

        yield return request.SendWebRequest();
        CompleteRequest(request, onSuccess, onFailure);
    }

    IEnumerator PostJson(string path, object payload, Action<string> onSuccess, Action onFailure)
    {
        string json = JsonUtility.ToJson(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new UnityWebRequest(BuildUrl(path), UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        ApplyAuthHeader(request);

        yield return request.SendWebRequest();
        CompleteRequest(request, onSuccess, onFailure);
    }

    void CompleteRequest(UnityWebRequest request, Action<string> onSuccess, Action onFailure)
    {
        if (request.result == UnityWebRequest.Result.Success)
        {
            onSuccess?.Invoke(request.downloadHandler.text);
            return;
        }

        FlowGuard.Error(
            FlowGuard.TagRestApi,
            $"{request.method} {request.url} failed: {request.responseCode} {request.error}",
            this
        );
        onFailure?.Invoke();
    }

    void ApplyAuthHeader(UnityWebRequest request)
    {
        if (HasAccessToken)
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
    }

    string BuildUrl(string path)
    {
        string root = string.IsNullOrWhiteSpace(RuntimeMode.BackendUrl)
            ? baseUrl
            : RuntimeMode.BackendUrl;

        return root.TrimEnd('/') + "/" + path.TrimStart('/');
    }

    static T ParseOrFallback<T>(string json, T fallback)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
            return fallback;

        T parsed = JsonUtility.FromJson<T>(json);
        return parsed == null ? fallback : parsed;
    }

    public bool ValidateCharacterOwnership(
        PlayerAccountSnapshot account,
        int characterId,
        CharacterDefinition definition
    )
    {
        if (definition != null && definition.DefaultOwned)
            return true;

        if (account?.ownedCharacterIds == null)
            return false;

        for (int i = 0; i < account.ownedCharacterIds.Length; i++)
        {
            if (account.ownedCharacterIds[i] == characterId)
                return true;
        }

        // TODO[REST_API] Server-side validate khi mua / ready online.
        return PlayerPrefs.GetInt(MatchSessionBroker.GetOwnedKey(characterId), 0) == 1;
    }

    [Serializable]
    class AuthSteamRequest
    {
        public string steamTicket;
    }

    [Serializable]
    class AuthSteamResponse
    {
        public string accessToken;
        public PlayerAccountSnapshot account;
    }

    [Serializable]
    class PurchaseCharacterRequest
    {
        public int characterId;
    }

    [Serializable]
    class PurchaseCharacterResponse
    {
        public bool success;
        public PlayerAccountSnapshot account;
        public string error;
    }

    [Serializable]
    class CreateMatchRequest
    {
        public string roomName;
        public string mapName;
        public int maxPlayers;
        public int characterId;
        public string displayName;
    }

    [Serializable]
    class CreateMatchResponse
    {
        public bool success;
        public MatchServerAllocation allocation;
        public string error;
    }

    [Serializable]
    class ServerReadyRequest
    {
        public string edgegapDeploymentId;
        public int port;
    }
}
