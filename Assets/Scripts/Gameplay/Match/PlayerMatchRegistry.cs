using System.Collections.Generic;
using PurrNet;

/// <summary>
/// Server-side map PlayerID → runtime player components.
/// </summary>
public sealed class PlayerMatchRegistry
{
    readonly Dictionary<PlayerID, PlayerRuntimeEntry> byPlayerId = new();
    readonly Dictionary<PlayerID, LeaderBoardData> lockedResultsByPlayerId = new();

    public void Register(PlayerID playerId, PlayerRuntimeEntry entry)
    {
        if (entry == null)
            return;

        byPlayerId[playerId] = entry;
    }

    public void Unregister(PlayerID playerId)
    {
        byPlayerId.Remove(playerId);
    }

    public void LockResult(PlayerID playerId, bool disconnected)
    {
        if (lockedResultsByPlayerId.ContainsKey(playerId))
            return;

        if (!byPlayerId.TryGetValue(playerId, out PlayerRuntimeEntry entry))
            return;

        LeaderBoardData data = BuildEntry(entry, disconnected);
        if (data.slotIndex < 0 && string.IsNullOrWhiteSpace(data.userId))
            return;

        lockedResultsByPlayerId[playerId] = data;
    }

    public bool TryGet(PlayerID playerId, out PlayerRuntimeEntry entry)
    {
        return byPlayerId.TryGetValue(playerId, out entry);
    }

    public int Count => byPlayerId.Count;

    public int CountNotEliminated()
    {
        int count = 0;

        foreach (PlayerRuntimeEntry entry in byPlayerId.Values)
        {
            if (entry?.Infor != null && !entry.Infor.IsEliminated)
                count++;
        }

        return count;
    }
    /// <summary>Server: chốt điểm survive + snapshot leaderboard.</summary>
    public List<LeaderBoardData> BuildLeaderBoardEntries(System.Action<PlayerRuntimeEntry> onSurvivorScored)
    {
        List<LeaderBoardData> result = new List<LeaderBoardData>();

        foreach (LeaderBoardData lockedResult in lockedResultsByPlayerId.Values)
            result.Add(lockedResult);

        foreach (KeyValuePair<PlayerID, PlayerRuntimeEntry> item in byPlayerId)
        {
            if (lockedResultsByPlayerId.ContainsKey(item.Key))
                continue;

            PlayerRuntimeEntry entry = item.Value;

            if (entry?.Infor == null || entry.BoardState == null)
                continue;

            if (!entry.Infor.IsEliminated)
                onSurvivorScored?.Invoke(entry);

            result.Add(BuildEntry(entry, disconnected: false));
        }

        result.Sort(
            (p1, p2) =>
            {
                if (p1.score == p2.score)
                    return p2.kills.CompareTo(p1.kills);

                return p2.score.CompareTo(p1.score);
            }
        );

        return result;
    }

    static LeaderBoardData BuildEntry(PlayerRuntimeEntry entry, bool disconnected)
    {
        if (entry?.Infor == null)
            return new LeaderBoardData { slotIndex = -1, disconnected = disconnected };

        return new LeaderBoardData
        {
            slotIndex = entry.BoardState != null ? entry.BoardState.SlotIndex : entry.Infor.PlayerIndex,
            userId = entry.Infor.UserId,
            name = entry.Infor.PlayerName,
            kills = entry.Infor.Kills,
            deaths = entry.Infor.Deaths,
            score = entry.Infor.Score,
            disconnected = disconnected,
        };
    }
}

public sealed class PlayerRuntimeEntry
{
    public PlayerID PlayerId;
    public PlayerController Controller;
    public PlayerInfor Infor;
    public PlayerBoardState BoardState;
    
}
public struct LeaderBoardData
{
    public int slotIndex;
    public string userId;
    public string name;
    public int kills;
    public int deaths;
    public int score;
    public bool disconnected;
}
