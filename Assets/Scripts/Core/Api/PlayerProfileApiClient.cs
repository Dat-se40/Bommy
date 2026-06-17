using System;
using UnityEngine;

/// <summary>
/// Provider for fetching and validating player profile data.
/// </summary>
public class PlayerProfileApiClient : MonoBehaviour
{
    [Header("Offline Fallback")]
    [SerializeField] private SamplePlayerDataSheet offlineSample;

    public void FetchLocalAccount(Action<PlayerAccountSnapshot> onCompleted)
    {
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

        return PlayerPrefs.GetInt(MatchSessionBroker.GetOwnedKey(characterId), 0) == 1;
    }
}
