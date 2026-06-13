using UnityEngine;
using UnityEngine.U2D.Animation;

public class PlayerSkinApplier : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private CharacterDatabase characterDatabase;

    [Header("Target")]
    [SerializeField] private SpriteLibrary spriteLibrary;

    /// <summary>
    /// Áp visual của nhân vật theo characterId.
    /// Mỗi màu/skin đang được tính là một CharacterDefinition riêng.
    /// </summary>
    public void ApplyCharacterVisual(int characterId)
    {
        if (characterDatabase == null)
        {
            Debug.LogWarning("[FLOW:SETUP] Cannot apply character visual: CharacterDatabase is null.");
            return;
        }

        CharacterDefinition character = characterDatabase.GetById(characterId);

        if (character == null)
        {
            Debug.LogWarning("[FLOW:SETUP] Cannot apply character visual: characterId not found: " + characterId);
            return;
        }

        if (character.SpriteLibrary == null)
        {
            Debug.LogWarning("[FLOW:SETUP] Cannot apply character visual: SpriteLibrary is null. characterId=" + characterId);
            return;
        }

        if (spriteLibrary == null)
            spriteLibrary = GetComponentInChildren<SpriteLibrary>();

        if (spriteLibrary == null)
        {
            Debug.LogWarning("[FLOW:SETUP] Cannot apply character visual: target SpriteLibrary component is null.");
            return;
        }

        spriteLibrary.spriteLibraryAsset = character.SpriteLibrary;
    }
}
