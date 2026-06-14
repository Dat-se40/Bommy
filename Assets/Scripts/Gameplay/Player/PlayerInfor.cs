using PurrNet;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gameplay authority trên server — HP, lives, bomb stats.
/// Visual/HUD sync qua PlayerBoardState SyncVar; hiệu ứng xem PlayerVisualFeedback.
/// </summary>
public class PlayerInfor : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField]
    private int playerIndex;

    [SerializeField]
    private int characterId;

    [SerializeField]
    private int catalogIndex;

    [SerializeField]
    private string playerName = "Player";

    [SerializeField]
    private bool isLocalPlayer = true;

    [Header("Movement")]
    [SerializeField]
    private float moveSpeed = 4f;
    private float maxMoveSpeed = 7f; 
    [Header("HP")]
    [SerializeField]
    private int maxHp = 3;

    [SerializeField]
    private int currentHp = 3;

    [Header("Lives")]
    [SerializeField]
    private int maxLives = 3;

    [SerializeField]
    private int currentLives = 3;

    [Header("Bomb")]
    [SerializeField]
    private int maxBombs = 1;

    [SerializeField]
    private int bombRange = 2;

    [Header("Match Stats")]
    [SerializeField]
    private int gold;

    [SerializeField]
    private int score;

    [SerializeField]
    private int kills;

    [SerializeField]
    private int deaths;

    [Header("Combat Timing")]
    [SerializeField]
    private float hitInvincibilityDuration = 0.45f;

    [SerializeField]
    private float deathResolveDuration = 0.55f;

    [SerializeField]
    private int shield;

    public int Shield => shield;
    public int PlayerIndex => playerIndex;
    public int CharacterId => characterId;
    public int CatalogIndex => catalogIndex;
    public string PlayerName => playerName;
    public bool IsLocalPlayer => isLocalPlayer;

    public float MoveSpeed => moveSpeed;
    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;

    public int MaxLives => maxLives;
    public int CurrentLives => currentLives;

    public int MaxBombs => maxBombs;
    public int BombRange => bombRange;

    public int Gold => gold;
    public int Score => score;
    public int Kills => kills;
    public int Deaths => deaths;

    public bool IsDead => currentHp <= 0;
    public bool IsEliminated => currentLives <= 0;

    bool isInvincible;
    bool isDeathPending;
    Coroutine invincibilityRoutine;
    Coroutine deathResolveRoutine;

    private PurrNet.PlayerID attackedBy; 
    void OnDisable()
    {
        StopAllCombatRoutines();
    }

    void Start()
    {
        if (isLocalPlayer)
            ApplyLocalProfileFromBroker();

        ResetForMatch();
    }

    public void ApplyMatchProfile(PlayerMatchProfile profile)
    {
        playerIndex = profile.slotIndex;
        characterId = profile.characterId;
        catalogIndex = profile.catalogIndex;
        playerName = profile.displayName;
        maxHp = profile.hp;
        maxBombs = profile.bomb;
        moveSpeed = profile.speed;

        if (TryGetComponent(out MovementController move))
            move.SetSpeed(profile.speed);

        Debug.Log($"[PLayer Infor] player {profile.displayName} completed apply match profile");
    }

    void ApplyLocalProfileFromBroker()
    {
        PlayerMatchProfile profile = MatchSessionBroker.GetLocalPlayer();

        if (profile.characterId <= 0)
            MatchSessionBroker.LoadLocalFromPlayerPrefs(MatchSessionBroker.CharacterCatalog);

        profile = MatchSessionBroker.GetLocalPlayer();

        if (profile.characterId > 0)
            ApplyMatchProfile(profile);
        else
            LoadSelectedCharacterStatsLegacy();
    }

    void LoadSelectedCharacterStatsLegacy()
    {
        playerName = PlayerPrefs.GetString("SelectedCharacterName", playerName);
        characterId = PlayerPrefs.GetInt("SelectedCharacterId", characterId);
        catalogIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", catalogIndex);
        maxHp = PlayerPrefs.GetInt("SelectedCharacterHp", maxHp);
        maxBombs = PlayerPrefs.GetInt("SelectedCharacterBomb", maxBombs);
    }

    public void ResetForMatch()
    {
        StopAllCombatRoutines();
        isInvincible = false;
        isDeathPending = false;

        currentHp = maxHp;
        currentLives = maxLives;
        shield = 0;

        gold = 0;
        score = 0;
        kills = 0;
        deaths = 0;

        PublishBoardState();
    }

    void PublishBoardState()
    {
        if (TryGetComponent(out PlayerBoardState boardState))
            boardState.PublishFromInfor(this);
    }

    public void TakeDamage(int damage)
    {
        if (IsEliminated || isInvincible || isDeathPending)
            return;

        if (damage <= 0)
            return;
      //  attackedBy = attacker.Value; 
        if (shield <= 0)
        {
            currentHp -= damage;
            BeginHitInvincibility();
        }
        else if (damage >= shield)
        {
            int overflow = damage - shield;
            shield = 0;
            currentHp -= overflow;
            BeginHitInvincibility();
        }
        else
        {
            shield -= damage;
        }

        if (currentHp <= 0)
        {
            currentHp = 0;
            PublishBoardState();
            BeginDeathResolve();
            return;
        }
        PublishBoardState();
    }

    void BeginHitInvincibility()
    {
        if (invincibilityRoutine != null)
            StopCoroutine(invincibilityRoutine);

        invincibilityRoutine = StartCoroutine(HitInvincibilityRoutine());
    }

    IEnumerator HitInvincibilityRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(hitInvincibilityDuration);
        isInvincible = false;
        invincibilityRoutine = null;
    }

    void BeginDeathResolve()
    {
        if (deathResolveRoutine != null)
            return;

        isInvincible = true;
        isDeathPending = true;
        deathResolveRoutine = StartCoroutine(DeathResolveRoutine());
    }

    IEnumerator DeathResolveRoutine()
    {
        yield return new WaitForSeconds(deathResolveDuration);
        isDeathPending = false;
        isInvincible = false;
        deathResolveRoutine = null;
        LoseLife();
    }

    void LoseLife()
    {
        deaths++;
        currentLives--;

        if (currentLives <= 0)
        {
            currentLives = 0;
            Eliminate();
            return;
        }

        Respawn();
    }

    void Respawn()
    {
        currentHp = maxHp;

        if (TryGetComponent(out MovementController move))
            move.SnapToNearestValidCell();

        Debug.Log(playerName + " respawned. Lives left: " + currentLives);
        PublishBoardState();
    }

    void Eliminate()
    {
        Debug.Log(playerName + " eliminated.");
        PublishBoardState();
        gameObject.SetActive(false);
    }

    public void Heal(int amount)
    {
        if (IsEliminated)
            return;

        if (amount <= 0)
            return;

        currentHp += amount;

        if (currentHp > maxHp)
            currentHp = maxHp;

        PublishBoardState();
    }

    public void AddLife(int amount)
    {
        if (amount <= 0)
            return;

        currentLives += amount;

        if (currentLives > maxLives)
            currentLives = maxLives;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        gold += amount;
        AddScore(amount * 2);
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
            return;

        score += amount;
        PublishBoardState();
    }

    public void AddKill()
    {
        kills++;
        AddScore(300);
        PublishBoardState();
    }

    public void AddBombCapacity(int amount)
    {
        if (amount <= 0)
            return;

        maxBombs += amount;
    }

    public void AddBombRange(int amount)
    {
        if (amount <= 0)
            return;

        bombRange += amount;
    }

    public void AddShield(int amount)
    {
        if (amount == 0)
            return;

        int maxShield = Mathf.Max(1, maxHp / 2);
        shield = Mathf.Clamp(shield + amount, 0, maxShield);
        PublishBoardState();
    }

    public void AddSpeed(float amount)
    {
        if (amount <= 0)
            return;

        // Giới hạn tốc độ lại
        moveSpeed = Mathf.Clamp(moveSpeed + amount, moveSpeed, maxMoveSpeed);
        if (TryGetComponent(out MovementController move))
            move.SetSpeed(moveSpeed);
    }

    void StopAllCombatRoutines()
    {
        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
            invincibilityRoutine = null;
        }

        if (deathResolveRoutine != null)
        {
            StopCoroutine(deathResolveRoutine);
            deathResolveRoutine = null;
        }
    }
}
