using System;

[Serializable]
public struct MatchJoinPayload
{
    public string matchId;
    public string matchToken;
    public int characterId;
    public int catalogIndex;
    public string displayName;
    public int hp;
    public int bomb;
    public int speed;

    public static MatchJoinPayload FromSession(MatchServerAllocation allocation, PlayerMatchProfile profile)
    {
        return new MatchJoinPayload
        {
            matchId = allocation?.matchId,
            matchToken = allocation?.matchToken,
            characterId = profile.characterId,
            catalogIndex = profile.catalogIndex,
            displayName = profile.displayName,
            hp = profile.hp,
            bomb = profile.bomb,
            speed = profile.speed
        };
    }

    public PlayerMatchProfile ToProfile()
    {
        return new PlayerMatchProfile
        {
            slotIndex = 0,
            characterId = characterId,
            catalogIndex = catalogIndex,
            displayName = displayName,
            hp = hp,
            bomb = bomb,
            speed = speed,
            isLocal = false
        };
    }
}
