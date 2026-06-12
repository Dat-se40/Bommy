using UnityEngine;

public class PlayerInfor : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private int playerIndex;
    [SerializeField] private int characterId;
    [SerializeField] private int catalogIndex;
    [SerializeField] private string playerName = "Player";
    [SerializeField] private bool isLocalPlayer = true;

    [Header("HP")]
    [SerializeField] private int maxHp = 3;
    [SerializeField] private int currentHp = 3;

    [Header("Lives")]
    [SerializeField] private int maxLives = 3;
    [SerializeField] private int currentLives = 3;

    [Header("Bomb")]
    [SerializeField] private int maxBombs = 1;
    [SerializeField] private int bombRange = 2;

    [Header("Match Stats")]
    [SerializeField] private int gold;
    [SerializeField] private int score;
    [SerializeField] private int kills;
    [SerializeField] private int deaths;

    public int PlayerIndex => playerIndex;
    public int CharacterId => characterId;
    public int CatalogIndex => catalogIndex;
    public string PlayerName => playerName;
    public bool IsLocalPlayer => isLocalPlayer;

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

    private void Start()
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

        if(TryGetComponent(out MovementController move)) 
        {
            move.SetSpeed(profile.speed);
        }
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
        currentHp = maxHp;
        currentLives = maxLives;

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
        if (IsEliminated)
            return;

        if (damage <= 0)
            return;

        currentHp -= damage;

        if (currentHp <= 0)
        {
            currentHp = 0;
            LoseLife();
        }

        PublishBoardState();
    }

    private void LoseLife()
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

    private void Respawn()
    {
        currentHp = maxHp;

        // Sau này có spawn point thì set vị trí ở đây.
        // transform.position = spawnPoint.position;

        Debug.Log(playerName + " respawned. Lives left: " + currentLives);
        PublishBoardState();
    }

    private void Eliminate()
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
}
