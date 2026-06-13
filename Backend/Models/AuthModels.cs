namespace Backend.Models;

public sealed record AuthDevRequest(string? DevId, string? DisplayName);
public sealed record AuthSteamRequest(string? SteamTicket);
public sealed record AuthResponse(string AccessToken, PlayerAccountSnapshot Account);
public sealed record ErrorResponse(string Error);
