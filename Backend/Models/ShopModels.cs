namespace Backend.Models;

public sealed record PurchaseCharacterRequest(int CharacterId);
public sealed record PurchaseCharacterResponse(
    bool Success,
    PlayerAccountSnapshot? Account,
    string? Error
);
