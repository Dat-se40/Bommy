using System;
using PurrNet;
using UnityEngine;

/// <summary>
/// State sync cho board HUD — server authority.
/// Mỗi player prefab có một instance; slot UI subscribe riêng.
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
    public bool IsEliminated => isEliminated.value;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        slotIndex.onChanged += OnAnyChanged;
        characterId.onChanged += OnAnyChanged;
        catalogIndex.onChanged += OnAnyChanged;
        displayName.onChanged += OnAnyChanged;
        currentHp.onChanged += OnAnyChanged;
        maxHp.onChanged += OnAnyChanged;
        currentLives.onChanged += OnAnyChanged;
        maxLives.onChanged += OnAnyChanged;
        score.onChanged += OnAnyChanged;
        kills.onChanged += OnAnyChanged;
        isEliminated.onChanged += OnAnyChanged;
    }

    protected override void OnDespawned()
    {
        slotIndex.onChanged -= OnAnyChanged;
        characterId.onChanged -= OnAnyChanged;
        catalogIndex.onChanged -= OnAnyChanged;
        displayName.onChanged -= OnAnyChanged;
        currentHp.onChanged -= OnAnyChanged;
        maxHp.onChanged -= OnAnyChanged;
        currentLives.onChanged -= OnAnyChanged;
        maxLives.onChanged -= OnAnyChanged;
        score.onChanged -= OnAnyChanged;
        kills.onChanged -= OnAnyChanged;
        isEliminated.onChanged -= OnAnyChanged;

        base.OnDespawned();
    }

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
        isEliminated.value = infor.IsEliminated;
    }

    void OnAnyChanged(int _) => Changed?.Invoke();
    void OnAnyChanged(string _) => Changed?.Invoke();
    void OnAnyChanged(bool _) => Changed?.Invoke();
}
