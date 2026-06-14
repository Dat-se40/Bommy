using UnityEngine;
using UnityEngine.U2D.Animation;

/// <summary>
/// Đổi màu/skin bằng cách gán SpriteLibraryAsset tương ứng từ CharacterDatabase.
/// Phải được gọi trên mọi client khi characterId đã replicate — không chỉ lúc server spawn.
/// </summary>
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
        CharacterDefinition character = ResolveDefinition(characterId);

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
            spriteLibrary = GetComponentInChildren<SpriteLibrary>(true);

        if (spriteLibrary == null)
        {
            Debug.LogWarning("[FLOW:SETUP] Cannot apply character visual: target SpriteLibrary component is null.");
            return;
        }

        spriteLibrary.spriteLibraryAsset = character.SpriteLibrary;
        RefreshSpriteResolvers();
    }

    CharacterDefinition ResolveDefinition(int characterId)
    {
        CharacterDatabase database = characterDatabase != null
            ? characterDatabase
            : MatchSessionBroker.CharacterCatalog;

        if (database == null)
        {
            Debug.LogWarning("[FLOW:SETUP] Cannot apply character visual: CharacterDatabase is null.");
            return null;
        }

        return database.GetById(characterId);
    }

    void RefreshSpriteResolvers()
    {
        SpriteResolver[] resolvers = GetComponentsInChildren<SpriteResolver>(true);

        for (int i = 0; i < resolvers.Length; i++)
            resolvers[i].ResolveSpriteToSpriteRenderer();
    }
}