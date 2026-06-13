using UnityEngine;

/// <summary>
/// Áp PlayerMatchProfile lên player vừa spawn local/remote.
/// </summary>
public class PlayerSpawnSetup : MonoBehaviour
{
    public void Apply(PlayerMatchProfile profile)
    {
        // Áp visual trước để player vừa spawn đã đúng màu/skin.
        PlayerSkinApplier skinApplier = GetComponentInChildren<PlayerSkinApplier>();

        if (skinApplier != null)
            skinApplier.ApplyCharacterVisual(profile.characterId);

        if (TryGetComponent(out PlayerInfor playerInfor))
            playerInfor.ApplyMatchProfile(profile);

        if (TryGetComponent(out PlayerBoardState boardState))
            boardState.InitializeFromProfile(profile);
    }
}
