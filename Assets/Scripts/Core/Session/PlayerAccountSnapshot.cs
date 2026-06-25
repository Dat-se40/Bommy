using System;

/// <summary>
/// Server-owned player progression returned by Nakama RPCs.
/// </summary>
[Serializable]
public class PlayerAccountSnapshot
{
    public int schemaVersion = 1;
    public int gold = 850;
    public int experience;
    public int level = 1;
    public int[] ownedCharacterIds = { 1 };
    public int selectedCharacterId = 1;
}
