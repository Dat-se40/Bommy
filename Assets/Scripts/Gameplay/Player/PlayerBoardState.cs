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
    readonly SyncVar<string> displayName = new("Player");
    readonly SyncVar<int> currentHp = new();
    readonly SyncVar<int> maxHp = new();
    readonly SyncVar<int> currentLives = new();
    readonly SyncVar<int> maxLives = new();
    readonly SyncVar<int> score = new();
    readonly SyncVar<int> kills = new();
    readonly SyncVar<int> shield = new();
    readonly SyncVar<bool> isEliminated = new();
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
        kills.onChanged += OnAnyChanged;
        shield.onChanged += OnAnyChanged;
        isEliminated.onChanged += OnAnyChanged;

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
        kills.onChanged -= OnAnyChanged;
        shield.onChanged -= OnAnyChanged;
        isEliminated.onChanged -= OnAnyChanged;

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
        maxLives.value = 3;
        currentLives.value = 3;
        score.value = 0;
        kills.value = 0;
        shield.value = 0;
        isEliminated.value = false;
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
        kills.value = infor.Kills;
        shield.value = infor.Shield;
        isEliminated.value = infor.IsEliminated;
    }

    void OnAnyChanged(int _) => Changed?.Invoke();
    void OnAnyChanged(string _) => Changed?.Invoke();
    void OnAnyChanged(bool _) => Changed?.Invoke();

}
