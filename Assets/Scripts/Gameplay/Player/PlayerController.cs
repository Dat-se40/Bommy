using PurrNet;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlaceableKind
{
    Bomb = 0,
    Trap = 1,
}

public class PlayerController : NetworkBehaviour
{
    #region Variables

    [Header("Bomb")]
    [SerializeField] private BombController bombPrefab;
    [SerializeField] private ExplosionCreator explosionCreator;
    [SerializeField] private Transform bombParent;

    [Header("References")]
    [SerializeField] private MovementController movementController;
    [SerializeField] private PlayerInfor playerInfor;
    [SerializeField] private Grid grid;

    [Header("Effects")]
    [SerializeField] private PlayerEffects playerEffects;

    readonly SyncVar<int> activeBombs = new();
    readonly SyncVar<int> activeTraps = new();
    readonly SyncVar<bool> superPowerState = new(); 
    PlayerBoardState boardState;

    #endregion

    #region Unity Methods

    void Awake()
    {
        if (movementController == null)
            movementController = GetComponent<MovementController>();

        if (playerInfor == null)
            playerInfor = GetComponent<PlayerInfor>();

        if (playerEffects == null)
            playerEffects = GetComponent<PlayerEffects>();

        TryGetComponent(out boardState);
    }

    void Update()
    {
        if (!isOwner)
            return;

        if (!IsReady)
            return;

        if (playerInfor.IsDead)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (movementController.IsMoving)
            return;

        if (!MatchPhaseRules.CanPlaceBomb)
            return;

        Vector3Int cell = movementController.CurrentCell;

        if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
            RequestPlaceSkill(PlaceableKind.Bomb, cell);

        if (keyboard.qKey.wasPressedThisFrame)
            RequestPlaceSkill(PlaceableKind.Trap, cell);
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();

        if (isServer && owner.HasValue && playerInfor != null)
        {
            MatchGameplayAuthority authority = MatchGameplayAuthority.Instance;

            if (authority != null)
                authority.RegisterPlayer(owner.Value, this, playerInfor);
        }

        StartCoroutine(SetUpDI());
        RefreshBoardPlaceables();
    }

    protected override void OnDespawned()
    {
        if (isServer && owner.HasValue)
        {
            MatchGameplayAuthority authority = MatchGameplayAuthority.Instance;

            if (authority != null)
                authority.UnregisterPlayer(owner.Value);
        }

        base.OnDespawned();
    }

    #endregion

    #region Public Methods

    public void OnBombExploded()
    {
        if (!isServer)
            return;

        activeBombs.value--;

        if (activeBombs.value < 0)
            activeBombs.value = 0;

        RefreshBoardPlaceables();
    }

    public void OnTrapRemoved()
    {
        if (!isServer)
            return;

        activeTraps.value--;

        if (activeTraps.value < 0)
            activeTraps.value = 0;

        RefreshBoardPlaceables();
    }

    public void RefreshBoardPlaceables()
    {
        if (!isServer)
            return;

        if (boardState == null)
            TryGetComponent(out boardState);

        if (boardState == null)
            return;

        int trapCharges = playerEffects != null ? playerEffects.MossTrapSkillCount : 0;
        boardState.PublishPlaceables(activeBombs.value, activeTraps.value, trapCharges);
    }

    #endregion

    #region Private Methods

    bool IsReady =>
        movementController != null &&
        movementController.IsMapReady &&
        grid != null &&
        explosionCreator != null;

    [ServerRpc(requireOwnership: false)]
    void RequestPlaceSkill(PlaceableKind kind, Vector3Int cell, RPCInfo rpcInfo = default)
    {
        if (!isServer)
            return;

        if (!TryValidatePlaceRequest(kind, cell, out Vector3 worldPosition))
            return;

        switch (kind)
        {
            case PlaceableKind.Bomb:
                PlaceBombServer(worldPosition, cell, rpcInfo);
                break;

            case PlaceableKind.Trap:
                PlaceTrapServer(worldPosition, cell, rpcInfo);
                break;
        }
    }

    /// <summary>
    /// Kiểm tra chung (phase, ô, cản) + điều kiện theo loại bomb/trap.
    /// </summary>
    bool TryValidatePlaceRequest(PlaceableKind kind, Vector3Int cell, out Vector3 worldPosition)
    {
        worldPosition = default;

        if (!MatchPhaseRules.CanPlaceBomb)
            return false;

        if (movementController == null || grid == null)
            return false;

        if (cell != movementController.CurrentCell)
            return false;

        if (!movementController.CanPlaceBombAtCell(cell))
            return false;

        worldPosition = grid.GetCellCenterWorld(cell);

        switch (kind)
        {
            case PlaceableKind.Bomb:
                return CanPlaceBombServer();

            case PlaceableKind.Trap:
                return CanPlaceTrapServer();

            default:
                return false;
        }
    }

    bool CanPlaceBombServer()
    {
        if (bombPrefab == null)
        {
            Debug.LogWarning("Bomb Prefab is not assigned.");
            return false;
        }

        if (explosionCreator == null)
        {
            Debug.LogWarning("Explosion Creator is not assigned.");
            return false;
        }

        int maxBombs = playerInfor != null ? playerInfor.MaxBombs : 1;

        return activeBombs.value < maxBombs;
    }

    bool CanPlaceTrapServer()
    {
        if (playerEffects == null || playerEffects.MossTrapSkillCount <= 0)
            return false;

        if (!TryGetActiveEffectTemplate(EffectType.MossTrap, out EffectTemplate effect))
            return false;

        if (effect.placedBombPrefab == null)
        {
            Debug.LogWarning("MossTrap effect does not have placedBombPrefab.");
            return false;
        }

        int maxTraps = playerInfor != null ? playerInfor.MaxBombs : 1;

        return activeTraps.value < maxTraps;
    }

    void PlaceBombServer(Vector3 worldPosition, Vector3Int cell, RPCInfo rpcInfo)
    {
        Debug.Log($"PlaceBomb called by {rpcInfo.sender.id} at cell {cell}");

        BombController bomb = Instantiate(
            bombPrefab,
            worldPosition,
            Quaternion.identity,
            bombParent
        );

        networkManager.Spawn(bomb.gameObject);
        bomb.Init(this, explosionCreator, cell);
        activeBombs.value++;
        RefreshBoardPlaceables();
    }

    void PlaceTrapServer(Vector3 worldPosition, Vector3Int cell, RPCInfo rpcInfo)
    {
        if (!TryGetActiveEffectTemplate(EffectType.MossTrap, out EffectTemplate effect))
            return;

        Debug.Log($"Place moss trap called by {rpcInfo.sender.id} at cell {cell}");

        GameObject trapObject = Instantiate(
            effect.placedBombPrefab,
            worldPosition,
            Quaternion.identity,
            bombParent
        );

        networkManager.Spawn(trapObject);

        if (!trapObject.TryGetComponent(out MossTrapController trap))
        {
            Debug.LogWarning("MossTrap prefab missing MossTrapController.");
            return;
        }

        trap.Init(this, effect, cell);
        playerEffects.RemoveEarliestMossTrapSkill();
        activeTraps.value++;
        RefreshBoardPlaceables();
    }

    bool TryGetActiveEffectTemplate(EffectType effectType, out EffectTemplate template)
    {
        template = null;

        if (playerEffects == null)
            return false;

        return playerEffects.TryGetActiveEffectTemplate(effectType, out template);
    }

    IEnumerator SetUpDI()
    {
        while (MapRefs.Instance == null)
            yield return null;

        MapRefs map = MapRefs.Instance;
        Grid mapGrid = map.GetComponentInParent<Grid>();

        movementController.SetMapRefs(
            mapGrid,
            map.Playground,
            map.Indestructibles,
            map.Destructibles
        );
        grid = mapGrid;

        while (ExplosionCreator.Instance == null)
            yield return null;

        explosionCreator = ExplosionCreator.Instance;
        bombParent = explosionCreator.transform;
    }
    public void TakeSuperpower() 
    {
        superPowerState.value = true; 
    }
    #endregion
}
