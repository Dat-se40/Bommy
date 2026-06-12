using System;

/// <summary>
/// DTO từ REST API — tài khoản + tiến trình shop.
/// </summary>
[Serializable]
public class PlayerAccountSnapshot
{
    public string userId = "local_guest";
    public string steamId;
    public string displayName = "Player";
    public int gold = 850;
    public int level = 1;
    public int[] ownedCharacterIds = { 1 };
}
