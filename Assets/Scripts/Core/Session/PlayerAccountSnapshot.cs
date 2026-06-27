using System;

/// <summary>
/// Server-owned player progression returned by Nakama RPCs.
/// </summary>
[Serializable]
public class PlayerAccountSnapshot
{
    public int schemaVersion = 2;
    public int coins = 850;
    // Legacy alias kept while older UI/data sheets still refer to gold.
    public int gold = 850;
    public int trophies;
    public int experience;
    public int level = 1;
    public int[] ownedCharacterIds = { 1 };
    public int selectedCharacterId = 1;
    public PlayerMatchStatsSnapshot matchStats = new();

    public int SpendableCurrency => coins > 0 ? coins : gold;
}

[Serializable]
public class PlayerMatchStatsSnapshot
{
    public int matchesPlayed;
    public int wins;
    public int kills;
    public int deaths;
}

[Serializable]
public struct MatchRewardPreview
{
    public int coinsDelta;
    public int trophiesDelta;
    public int experienceDelta;
    public int matchesPlayedDelta;
    public int winsDelta;
    public int killsDelta;
    public int deathsDelta;
}
