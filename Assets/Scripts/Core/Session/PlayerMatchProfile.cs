using System;

[Serializable]
public struct PlayerMatchProfile
{
    public int slotIndex;
    public int characterId;
    public int catalogIndex;

    public string displayName;

    public int hp;
    public int bomb;
    public int speed;

    public bool isLocal;

    /// <summary>
    /// Tạo profile từ CharacterDefinition để truyền giữa scene/network.
    /// Chỉ chứa metadata nhẹ, không chứa Sprite hoặc Prefab.
    /// </summary>
    public static PlayerMatchProfile FromDefinition(
        CharacterDefinition definition,
        int catalogIndex,
        int slotIndex,
        bool isLocal,
        string displayNameOverride = null
    )
    {
        if (definition == null)
            return default;

        string finalDisplayName = string.IsNullOrWhiteSpace(displayNameOverride)
            ? definition.CharacterName
            : displayNameOverride;

        return new PlayerMatchProfile
        {
            slotIndex = slotIndex,
            characterId = definition.CharacterId,
            catalogIndex = catalogIndex,
            displayName = finalDisplayName,
            hp = definition.Hp,
            bomb = definition.Bomb,
            speed = definition.Speed,
            isLocal = isLocal
        };
    }
}
