using System;
using UnityEngine;

/// <summary>
/// Module REST API — fetch + validate tài khoản từ BE.
/// </summary>
public class PlayerProfileApiClient : MonoBehaviour
{
    [Header("REST")]
    [SerializeField] private string baseUrl = "http://localhost:8080";

    [Header("Offline Fallback")]
    [SerializeField] private SamplePlayerDataSheet offlineSample;

    public void FetchLocalAccount(Action<PlayerAccountSnapshot> onCompleted)
    {
        // TODO[REST_API] Steam auth: lấy SteamId / session ticket từ Steamworks
        // TODO[REST_API] POST {baseUrl}/v1/auth/steam → nhận account token
        // TODO[REST_API] GET  {baseUrl}/v1/players/me (Authorization: Bearer ...)
        // TODO[REST_API] Parse JSON → PlayerAccountSnapshot
        // TODO[REST_API] On error → dùng offlineSample

        PlayerAccountSnapshot snapshot = offlineSample != null
            ? offlineSample.account
            : new PlayerAccountSnapshot();

        onCompleted?.Invoke(snapshot);
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
}
