using System.Collections.Generic;
using PurrNet;

/// <summary>
/// Server-side map PlayerID → runtime player components.
/// </summary>
public sealed class PlayerMatchRegistry
{
    readonly Dictionary<PlayerID, PlayerRuntimeEntry> byPlayerId = new();

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
}

public sealed class PlayerRuntimeEntry
{
    public PlayerController Controller;
    public PlayerInfor Infor;
    public PlayerBoardState BoardState;
}
