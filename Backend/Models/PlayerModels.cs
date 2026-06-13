namespace Backend.Models;

public sealed record PlayerAccountSnapshot(
    string UserId,
    string? SteamId,
    string DisplayName,
    int Gold,
    int Level,
    int[] OwnedCharacterIds
);

public sealed record PlayerMatchProfile(
    int SlotIndex,
    int CharacterId,
    int CatalogIndex,
    string DisplayName,
    int Hp,
    int Bomb,
    int Speed,
    bool IsLocal
);
