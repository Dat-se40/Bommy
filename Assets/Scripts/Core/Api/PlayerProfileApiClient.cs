using System;
using UnityEngine;

/// <summary>
/// Provider for fetching and validating player profile data.
/// </summary>
public class PlayerProfileApiClient : MonoBehaviour
{
    public void FetchLocalAccount(Action<PlayerAccountSnapshot> onCompleted)
    {
        onCompleted?.Invoke(PlayerProgressionService.Instance?.Current);
    }

    public bool ValidateCharacterOwnership(
        PlayerAccountSnapshot account,
        int characterId,
        CharacterDefinition definition
    )
    {
        if (account?.ownedCharacterIds == null)
            return false;

        for (int i = 0; i < account.ownedCharacterIds.Length; i++)
        {
            if (account.ownedCharacterIds[i] == characterId)
                return true;
        }

        return false;
    }
}
