using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

/// <summary>
/// Client facade for server-owned player progression RPCs.
/// </summary>
public sealed class PlayerProgressionService : MonoBehaviour
{
    const string SingletonName = "[PlayerProgressionService]";
    const string LegacyPurgeMarker = "Bommy.Nakama.LegacyProgressionPurged";
    const int ProgressionRpcTimeoutSeconds = 12;
    static readonly string[] LegacyKeys =
    {
        "PlayerGold",
        "PlayerLevel",
        "PlayerExp",
        "SelectedCharacterId",
        "SelectedCharacterIndex",
        "SelectedCharacterName",
        "SelectedCharacterHp",
        "SelectedCharacterBomb",
        "SelectedCharacterSpeed"
    };

    static PlayerProgressionService instance;

    public static event Action ProgressionChanged;
    public static PlayerProgressionService Instance => instance;
    public PlayerAccountSnapshot Current { get; private set; }
    public bool IsLoaded => Current != null;

    public static PlayerProgressionService EnsureExists()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<PlayerProgressionService>();

        if (instance != null)
            return instance;

        GameObject host = new(SingletonName);
        instance = host.AddComponent<PlayerProgressionService>();
        return instance;
    }

    public Task<PlayerAccountSnapshot> RefreshAsync()
    {
        return CallProgressionRpcAsync("get_player_progression", "{}");
    }

    public Task<PlayerAccountSnapshot> PurchaseCharacterAsync(int characterId)
    {
        return CallProgressionRpcAsync("purchase_character", BuildCharacterPayload(characterId));
    }

    public Task<PlayerAccountSnapshot> SelectCharacterAsync(int characterId)
    {
        return CallProgressionRpcAsync("select_character", BuildCharacterPayload(characterId));
    }

    public Task<PlayerAccountSnapshot> ApplyMatchRewardsAsync(MatchRewardPreview reward)
    {
        return CallProgressionRpcAsync("grant_match_rewards", BuildRewardPayload(reward));
    }

    public bool OwnsCharacter(int characterId)
    {
        int[] owned = Current?.ownedCharacterIds;

        if (owned == null)
            return false;

        for (int i = 0; i < owned.Length; i++)
        {
            if (owned[i] == characterId)
                return true;
        }

        return false;
    }

    public void Clear()
    {
        Current = null;
        ProgressionChanged?.Invoke();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async Task<PlayerAccountSnapshot> CallProgressionRpcAsync(string rpcId, string payload)
    {
        IApiRpc response = await AwaitProgressionRpcAsync(rpcId, payload);
        PlayerAccountSnapshot snapshot = JsonUtility.FromJson<PlayerAccountSnapshot>(response.Payload);

        if (snapshot == null || snapshot.schemaVersion <= 0)
            throw new InvalidOperationException("Nakama returned invalid player progression.");

        NormalizeSnapshot(snapshot);
        Current = snapshot;
        PurgeLegacyProgressionOnce();
        ProgressionChanged?.Invoke();
        return Current;
    }

    static async Task<IApiRpc> AwaitProgressionRpcAsync(string rpcId, string payload)
    {
        Task<IApiRpc> rpcTask = AuthService.GetOrCreate().RpcAsync(rpcId, payload);
        Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(ProgressionRpcTimeoutSeconds));

        Task completedTask = await Task.WhenAny(rpcTask, timeoutTask);
        if (completedTask != rpcTask)
            throw new TimeoutException("Progression server did not respond. Check backend and try again.");

        return await rpcTask;
    }

    static string BuildCharacterPayload(int characterId)
    {
        return "{\"characterId\":" + characterId + "}";
    }

    static string BuildRewardPayload(MatchRewardPreview reward)
    {
        return "{" +
            "\"coinsDelta\":" + reward.coinsDelta + "," +
            "\"trophiesDelta\":" + reward.trophiesDelta + "," +
            "\"experienceDelta\":" + reward.experienceDelta + "," +
            "\"matchesPlayedDelta\":" + reward.matchesPlayedDelta + "," +
            "\"winsDelta\":" + reward.winsDelta + "," +
            "\"killsDelta\":" + reward.killsDelta + "," +
            "\"deathsDelta\":" + reward.deathsDelta +
            "}";
    }

    static void NormalizeSnapshot(PlayerAccountSnapshot snapshot)
    {
        if (snapshot.matchStats == null)
            snapshot.matchStats = new PlayerMatchStatsSnapshot();

        if (snapshot.coins <= 0 && snapshot.gold > 0)
            snapshot.coins = snapshot.gold;

        snapshot.gold = snapshot.coins;
    }

    static void PurgeLegacyProgressionOnce()
    {
        if (PlayerPrefs.GetInt(LegacyPurgeMarker, 0) == 1)
            return;

        for (int i = 0; i < LegacyKeys.Length; i++)
            PlayerPrefs.DeleteKey(LegacyKeys[i]);

        for (int characterId = 1; characterId <= 5; characterId++)
            PlayerPrefs.DeleteKey("CharacterOwned_" + characterId);

        PlayerPrefs.SetInt(LegacyPurgeMarker, 1);
        PlayerPrefs.Save();
    }
}
