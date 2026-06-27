using PurrNet;
using System;

/// <summary>
/// Payload nhẹ đi qua scene / network. Chỉ gửi id + idx + tên;
/// sprite và prefab resolve qua CharacterDatabase.
/// </summary>
[Serializable]
public struct PlayerMatchProfile
{
    public int slotIndex;
    public int characterId;
    public int catalogIndex;
    public string displayName;
    public string userId;
    public int hp;
    public int bomb;
    public int speed;
    public bool isLocal;
    public PlayerID owner; 
    public static PlayerMatchProfile FromDefinition(
        CharacterDefinition definition,
        int catalogIndex,
        int slotIndex = 0,
        bool isLocal = true,
        string displayNameOverride = null,
        string userIdOverride = null
    )
    {
        if (definition == null)
            return default;

        return new PlayerMatchProfile
        {
            slotIndex = slotIndex,
            characterId = definition.CharacterId,
            catalogIndex = catalogIndex,
            displayName = string.IsNullOrWhiteSpace(displayNameOverride)
                ? definition.CharacterName
                : displayNameOverride,
            userId = userIdOverride ?? string.Empty,
            hp = definition.Hp,
            bomb = definition.Bomb,
            speed = definition.Speed,
            isLocal = isLocal
        };
    }
}
