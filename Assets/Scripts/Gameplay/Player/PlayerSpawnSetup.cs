using UnityEngine;

/// <summary>
/// Áp PlayerMatchProfile lên player vừa spawn local/remote.
/// </summary>
public class PlayerSpawnSetup : MonoBehaviour
{
    public void Apply(PlayerMatchProfile profile)
    {
        if (TryGetComponent(out PlayerInfor playerInfor))
            playerInfor.ApplyMatchProfile(profile);

        // characterId replicate → PlayerBoardState.TryApplyCharacterVisual trên mọi client.
        if (TryGetComponent(out PlayerBoardState boardState))
            boardState.InitializeFromProfile(profile);
    }
}
