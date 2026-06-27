using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

/// <summary>
/// GameAuth — authority tập trung cho combat/score/match end.
///
/// SETUP GameScene (bắt buộc, không AddComponent runtime):
/// 1. Tạo empty GameObject tên "MatchSystems" (cùng cấp Grid/Players, không phải con player).
/// 2. Gắn NetworkObject/Identity của PurrNet (giống ExplosionCreator).
/// 3. Gắn script này — phải có trên scene trước khi Play, host và client cùng scene.
///
/// ExplosionCreator / PlayerController chỉ gọi Submit* — không replicate qua bomb.
/// </summary>
public class MatchGameplayAuthority : NetworkBehaviour
{
    static MatchGameplayAuthority instance;

    public static MatchGameplayAuthority Instance
    {
        get
        {
            if (instance == null)
                instance = FindAnyObjectByType<MatchGameplayAuthority>();

            return instance;
        }
    }

    readonly PlayerMatchRegistry registry = new();
    readonly SyncList<MatchEvent> matchEvents = new();
    readonly SyncVar<int> matchPlayerCount = new();
    readonly SyncVar<bool> matchFinished = new();
    readonly SyncList<LeaderBoardData> leaderBoardData = new();
    public SyncList<MatchEvent> MatchEvents => matchEvents;
    public int MatchPlayerCount => matchPlayerCount.value;
    public bool IsMatchFinished => matchFinished.value;
    public SyncList<LeaderBoardData> LeaderBoardData => leaderBoardData;
    public event Action<bool> MatchFinishedChanged;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"{nameof(MatchGameplayAuthority)}: duplicate on '{name}'.", this);
            return;
        }

        instance = this;
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();

        matchFinished.onChanged += OnMatchFinishedReplicated;

        if (matchFinished.value)
            MatchFinishedChanged?.Invoke(true);
    }

    protected override void OnDespawned()
    {
        matchFinished.onChanged -= OnMatchFinishedReplicated;

        if (instance == this)
            instance = null;

        base.OnDespawned();
    }

    protected override void OnDestroy()
    {
        if (instance == this)
            instance = null;

        base.OnDestroy();
    }

    void OnMatchFinishedReplicated(bool finished)
    {
        MatchFinishedChanged?.Invoke(finished);
    }

    public int GetOnlinePlayer() => registry.Count;

    public void RegisterPlayer(PlayerID playerId, PlayerController controller, PlayerInfor infor)
    {
        if (!isServer || infor == null)
            return;

        PlayerBoardState boardState = null;
        if (controller != null)
            controller.TryGetComponent(out boardState);
        else
            infor.TryGetComponent(out boardState);

        registry.Register(
            playerId,
            new PlayerRuntimeEntry
            {
                PlayerId = playerId,
                Controller = controller,
                Infor = infor,
                BoardState = boardState,
            }
        );

        infor.SetAuthorityPlayerId(playerId);
        matchPlayerCount.value = registry.Count;

        FlowGuard.Info(
            FlowGuard.TagNetwork,
            $"Registered player id={playerId.id} → {infor.PlayerName}",
            this
        );
    }

    public void UnregisterPlayer(PlayerID playerId)
    {
        if (!isServer)
            return;

        if (ShouldLockDisconnectedResult())
            registry.LockResult(playerId, disconnected: true);

        registry.Unregister(playerId);
        matchPlayerCount.value = registry.Count;

        if (ShouldResolveMatchFromPlayerLoss())
            TryEndMatchFromPlayerLoss("disconnect");
    }

    public void SubmitAttack(AttackDTO attack)
    {
        if (!isServer)
            return;

        if (attack.damage <= 0)
            return;

        if (!registry.TryGet(attack.injured, out PlayerRuntimeEntry victim))
        {
            FlowGuard.Error(
                FlowGuard.TagNetwork,
                $"SubmitAttack: unknown victim id={attack.injured.id}",
                this
            );
            return;
        }

        bool hpDropped = victim.Infor.ApplyDamageFromAuthority(attack.damage, attack.attacker);
        PublishPlayer(victim);

        if (hpDropped)
            AppendEvent(MatchEventType.Damage, attack.attacker, attack.injured, attack.damage);
    }

    public void AwardKill(PlayerID killerId, PlayerID victimId)
    {
        if (!isServer)
            return;

        if (!registry.TryGet(killerId, out PlayerRuntimeEntry killer))
            return;

        killer.Infor.RecordKill();
        killer.Infor.AddScore(General.SCORE_ATTACK_PLAYER);
        PublishPlayer(killer);

        AppendEvent(MatchEventType.Kill, killerId, victimId, General.SCORE_KILL_BONUS);
    }

    public void NotifyPlayerEliminated(PlayerID eliminatedId)
    {
        if (!isServer)
            return;

        registry.LockResult(eliminatedId, disconnected: false);

        TryEndMatchFromPlayerLoss("elimination");
    }

    public void GrantScore(PlayerID playerId, int amount)
    {
        if (!isServer || amount <= 0)
            return;

        if (!registry.TryGet(playerId, out PlayerRuntimeEntry entry))
            return;

        entry.Infor.AddScore(amount);
        PublishPlayer(entry);

        AppendEvent(MatchEventType.ScoreGrant, playerId, playerId, amount);
    }

    public void PublishPlayer(PlayerRuntimeEntry entry)
    {
        if (!isServer || entry?.Infor == null || entry.BoardState == null)
            return;

        entry.BoardState.PublishFromInfor(entry.Infor);
    }

    void AppendEvent(MatchEventType type, PlayerID source, PlayerID target, int value)
    {
        matchEvents.Add(
            new MatchEvent
            {
                type = type,
                source = source,
                target = target,
                value = value,
            }
        );
    }
    /// <summary>
    /// Kết thúc match theo timer (zone shrink) hoặc gọi trực tiếp từ gameplay.
    /// Build leaderboard trước khi set matchFinished để UI nhận đủ dữ liệu.
    /// </summary>
    public void GameOver()
    {
        if (!isServer)
            return;

        EndMatch();
    }

    void EndMatch()
    {
        if (!isServer || matchFinished.value)
            return;

        BuildLeaderBoard();
        PublishLeaderBoard();
        matchFinished.value = true;
        SubmitSettlementAfterMatchEnd();

        AppendEvent(MatchEventType.GameOver, new PlayerID(0, true), new PlayerID(0, true), 0);
    }

    void TryEndMatchFromPlayerLoss(string reason)
    {
        if (!isServer || matchFinished.value)
            return;

        int survivors = registry.CountNotEliminated();
        Debug.Log("[MatchGameplayAuthority] Player loss check reason=" + reason + " survivors=" + survivors + " registered=" + registry.Count + ".", this);

        if (survivors > 1)
            return;

        EndMatch();
    }
    void BuildLeaderBoard()
    {
        if (!isServer)
            return;

        pendingLeaderBoard = registry.BuildLeaderBoardEntries(
            survivor =>
            {
                survivor.Infor.AddScore(General.SCORE_SURVIVE_BONUS);
                PublishPlayer(survivor);
            }
        );
    }

    List<LeaderBoardData> pendingLeaderBoard;

    void PublishLeaderBoard()
    {
        if (!isServer || pendingLeaderBoard == null)
            return;

        leaderBoardData.Clear();
        
        for (int i = 0; i < pendingLeaderBoard.Count; i++)
            leaderBoardData.Add(pendingLeaderBoard[i]);

        pendingLeaderBoard = null;
    }

    async void SubmitSettlementAfterMatchEnd()
    {
        if (!isServer || !DedicatedMatchRuntime.HasLaunchConfig)
            return;

        List<LeaderBoardData> snapshot = new();
        for (int i = 0; i < leaderBoardData.Count; i++)
            snapshot.Add(leaderBoardData[i]);

        try
        {
            Debug.Log("[MatchGameplayAuthority] Settlement requested for match " + DedicatedMatchRuntime.MatchId + " with " + snapshot.Count + " result(s).", this);
            MatchSettlementResponse settlement = await DedicatedMatchRuntime.SettleAndReleaseAsync(snapshot);
            if (settlement != null && settlement.success)
                Debug.Log("[MatchGameplayAuthority] Settled match " + settlement.matchId + " with " + snapshot.Count + " result(s).", this);
        }
        catch (Exception exception)
        {
            Debug.LogError("[MatchGameplayAuthority] Match settlement failed: " + exception.Message, this);
        }
    }

    bool ShouldLockDisconnectedResult()
    {
        if (matchFinished.value)
            return true;

        MatchPhaseBroadcast broadcast = MatchPhaseBroadcast.Instance;
        if (broadcast == null)
            return false;

        return broadcast.CurrentPhase == MatchPhaseKind.Gameplay
            || broadcast.CurrentPhase == MatchPhaseKind.ZoneShrink;
    }

    bool ShouldResolveMatchFromPlayerLoss()
    {
        if (matchFinished.value)
            return false;

        MatchPhaseBroadcast broadcast = MatchPhaseBroadcast.Instance;
        if (broadcast == null)
            return false;

        return broadcast.CurrentPhase == MatchPhaseKind.Gameplay
            || broadcast.CurrentPhase == MatchPhaseKind.ZoneShrink;
    }
}
