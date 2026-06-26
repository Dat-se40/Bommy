using System;
using PurrNet;
using UnityEngine;

/// <summary>
/// State sync cho board HUD + visual feedback trên mọi client.
/// PlayerInfor (server) ghi stats → SyncVar replicate → PlayerBoardSlotUI / PlayerVisualFeedback subscribe.
/// </summary>
public class PlayerBoardState : NetworkBehaviour
{
    readonly SyncVar<int> slotIndex = new();
    readonly SyncVar<int> characterId = new();
    readonly SyncVar<int> catalogIndex = new();
    readonly SyncVar<string> displayName = new(General.PLAYER_DEFAULT_NAME);
    readonly SyncVar<int> currentHp = new();
    readonly SyncVar<int> maxHp = new();
    readonly SyncVar<int> currentLives = new();
    readonly SyncVar<int> maxLives = new();
    readonly SyncVar<int> score = new();
    readonly SyncVar<int> kills = new();
    readonly SyncVar<int> shield = new();
    readonly SyncVar<bool> isEliminated = new();
    readonly SyncVar<int> maxBombs = new(General.PLAYER_STATE_MIN_BOMBS);
    readonly SyncVar<int> activeBombs = new();
    readonly SyncVar<int> maxTrapSlots = new(General.PLAYER_STATE_MIN_TRAP_SLOTS);
    readonly SyncVar<int> activeTraps = new();
    readonly SyncVar<int> trapSkillCharges = new();
    readonly SyncVar<int> gold= new(); 
    public event Action Changed;

    public int SlotIndex => slotIndex.value;
    public int CharacterId => characterId.value;
    public int CatalogIndex => catalogIndex.value;
    public string DisplayName => displayName.value;
    public int CurrentHp => currentHp.value;
    public int MaxHp => maxHp.value;
    public int CurrentLives => currentLives.value;
    public int MaxLives => maxLives.value;
    public int Score => score.value;
    public int Kills => kills.value;
    public int Shield => shield.value;
    public bool IsEliminated => isEliminated.value;
    public int MaxBombs => maxBombs.value;
    public int ActiveBombs => activeBombs.value;
    public int AvailableBombs => Mathf.Max(0, maxBombs.value - activeBombs.value);
    public int MaxTrapSlots => maxTrapSlots.value;
    public int ActiveTraps => activeTraps.value;
    public int TrapSkillCharges => trapSkillCharges.value;
    public int Gold => gold.value; 
    protected override void OnSpawned()
    {
        base.OnSpawned();

        slotIndex.onChanged += OnSlotIndexChanged;
        slotIndex.onChanged += OnAnyChanged;
        characterId.onChanged += OnIdentityChanged;
        characterId.onChanged += OnAnyChanged;
        catalogIndex.onChanged += OnAnyChanged;
        displayName.onChanged += OnIdentityChanged;
        displayName.onChanged += OnAnyChanged;
        currentHp.onChanged += OnAnyChanged;
        maxHp.onChanged += OnAnyChanged;
        currentLives.onChanged += OnAnyChanged;
        maxLives.onChanged += OnAnyChanged;
        score.onChanged += OnAnyChanged;
        gold.onChanged += OnAnyChanged;
        kills.onChanged += OnAnyChanged;
        shield.onChanged += OnAnyChanged;
        isEliminated.onChanged += OnAnyChanged;
        maxBombs.onChanged += OnAnyChanged;
        activeBombs.onChanged += OnAnyChanged;
        maxTrapSlots.onChanged += OnAnyChanged;
        activeTraps.onChanged += OnAnyChanged;
        trapSkillCharges.onChanged += OnAnyChanged;

        TryRegisterWithHub();
        TryApplyCharacterVisual();
    }

    protected override void OnDespawned()
    {
        slotIndex.onChanged -= OnSlotIndexChanged;
        slotIndex.onChanged -= OnAnyChanged;
        characterId.onChanged -= OnIdentityChanged;
        characterId.onChanged -= OnAnyChanged;
        catalogIndex.onChanged -= OnAnyChanged;
        displayName.onChanged -= OnIdentityChanged;
        displayName.onChanged -= OnAnyChanged;
        currentHp.onChanged -= OnAnyChanged;
        maxHp.onChanged -= OnAnyChanged;
        currentLives.onChanged -= OnAnyChanged;
        maxLives.onChanged -= OnAnyChanged;
        score.onChanged -= OnAnyChanged;
        gold.onChanged -= OnAnyChanged;
        kills.onChanged -= OnAnyChanged;
        shield.onChanged -= OnAnyChanged;
        isEliminated.onChanged -= OnAnyChanged;
        maxBombs.onChanged -= OnAnyChanged;
        activeBombs.onChanged -= OnAnyChanged;
        maxTrapSlots.onChanged -= OnAnyChanged;
        activeTraps.onChanged -= OnAnyChanged;
        trapSkillCharges.onChanged -= OnAnyChanged;

        if (PlayerBoardHub.Instance != null)
            PlayerBoardHub.Instance.OnBoardStateDespawned(SlotIndex);

        base.OnDespawned();
    }

    void OnSlotIndexChanged(int _) => TryRegisterWithHub();
    void OnIdentityChanged(int _) => OnIdentityReplicated();
    void OnIdentityChanged(string _) => OnIdentityReplicated();

    void OnIdentityReplicated()
    {
        TryRegisterWithHub();
        TryApplyCharacterVisual();
    }

    void TryRegisterWithHub()
    {
        if (characterId.value <= 0)
            return;

        if (PlayerBoardHub.Instance == null)
            return;

        PlayerBoardHub.Instance.RegisterBoardState(this);
    }

    void TryApplyCharacterVisual()
    {
        if (characterId.value <= 0)
            return;

        PlayerSkinApplier skinApplier = GetComponent<PlayerSkinApplier>();

        if (skinApplier == null)
            skinApplier = GetComponentInChildren<PlayerSkinApplier>(true);

        if (skinApplier == null)
            return;

        skinApplier.ApplyCharacterVisual(characterId.value);
    }

    /// <summary>
    /// Server gọi trực tiếp sau networkManager.Spawn — không dùng ServerRpc ở đây.
    /// </summary>
    public void InitializeFromProfile(PlayerMatchProfile profile)
    {
        if (!isServer)
            return;

        slotIndex.value = profile.slotIndex;
        characterId.value = profile.characterId;
        catalogIndex.value = profile.catalogIndex;
        displayName.value = profile.displayName;
        maxHp.value = profile.hp;
        currentHp.value = profile.hp;
        maxLives.value = General.PLAYER_STATE_MATCH_MAX_LIVES;
        currentLives.value = General.PLAYER_STATE_MATCH_MAX_LIVES;
        score.value = 0;
        gold.value = 0; 
        kills.value = 0;
        shield.value = 0;
        isEliminated.value = false;
        maxBombs.value = Mathf.Max(General.PLAYER_STATE_MIN_BOMBS, profile.bomb);
        activeBombs.value = 0;
        maxTrapSlots.value = Mathf.Max(General.PLAYER_STATE_MIN_TRAP_SLOTS, profile.bomb);
        activeTraps.value = 0;
        trapSkillCharges.value = 0;
    }

    public void PublishFromInfor(PlayerInfor infor)
    {
        if (!isServer || infor == null)
            return;

        currentHp.value = infor.CurrentHp;
        maxHp.value = infor.MaxHp;
        currentLives.value = infor.CurrentLives;
        maxLives.value = infor.MaxLives;
        score.value = infor.Score;
        gold.value = infor.Gold; 
        kills.value = infor.Kills;
        shield.value = infor.Shield;
        isEliminated.value = infor.IsEliminated;
        maxBombs.value = Mathf.Max(General.PLAYER_STATE_MIN_BOMBS, infor.MaxBombs);
        maxTrapSlots.value = Mathf.Max(General.PLAYER_STATE_MIN_TRAP_SLOTS, infor.MaxBombs);
    }

    /// <summary>Server — đồng bộ bomb/trap đang dùng + charge skill lên HUD.</summary>
    public void PublishPlaceables(int activeBombCount, int activeTrapCount, int mossTrapCharges)
    {
        if (!isServer)
            return;

        activeBombs.value = Mathf.Max(0, activeBombCount);
        activeTraps.value = Mathf.Max(0, activeTrapCount);
        trapSkillCharges.value = Mathf.Max(0, mossTrapCharges);
    }

    void OnAnyChanged(int _) => Changed?.Invoke();
    void OnAnyChanged(string _) => Changed?.Invoke();
    void OnAnyChanged(bool _) => Changed?.Invoke();

}
