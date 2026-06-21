using PurrNet;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
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


    private int activeBombs;

    private void Awake()
    {
        if (movementController == null)
            movementController = GetComponent<MovementController>();

        if (playerInfor == null)
            playerInfor = GetComponent<PlayerInfor>();

        if (playerEffects == null)
            playerEffects = GetComponent<PlayerEffects>();

    }

    private void Update()
    {
        if (!isOwner) return;
        if (!IsReady) return;
        if (playerInfor.IsDead) return;
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
        {
            if (!MatchPhaseRules.CanPlaceBomb)
                return;

            if (movementController.IsMoving)
                return;

            RequestPlaceBomb(movementController.CurrentCell);
        }
    }

    bool IsReady =>
        movementController != null &&
        movementController.IsMapReady &&
        grid != null &&
        explosionCreator != null;

    [ServerRpc(requireOwnership: false)]
    void RequestPlaceBomb(Vector3Int bombCell, RPCInfo rpcInfo = default)
    {
        if (!isServer) return;

        if (!MatchPhaseRules.CanPlaceBomb)
            return;

        Debug.Log($"PlaceBomb called by {rpcInfo.sender.id} at cell {bombCell}");
        if (bombPrefab == null)
        {
            Debug.LogWarning("Bomb Prefab is not assigned.");
            return;
        }

        if (explosionCreator == null)
        {
            Debug.LogWarning("Explosion Creator is not assigned.");
            return;
        }

        if (movementController == null)
        {
            Debug.LogWarning("MovementController is not assigned.");
            return;
        }

        if (grid == null)
        {
            Debug.LogWarning("Grid is not assigned.");
            return;
        }

        int maxBombs = playerInfor != null ? playerInfor.MaxBombs : 1;

        if (activeBombs >= maxBombs)
            return;

        if (bombCell != movementController.CurrentCell)
            return;

        if (!movementController.CanPlaceBombAtCell(bombCell))
            return;

        Vector3 bombPosition = grid.GetCellCenterWorld(bombCell);

        if (TryPlaceMossTrap(bombPosition))
        {
            activeBombs++;
            return;
        }

        BombController bomb = Instantiate(
            bombPrefab,
            bombPosition,
            Quaternion.identity,
            bombParent
        );

        networkManager.Spawn(bomb.gameObject);
        bomb.Init(this, explosionCreator, bombCell);

        activeBombs++;
    }

    public void OnBombExploded()
    {
        activeBombs--;

        if (activeBombs < 0)
            activeBombs = 0;
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

    private IEnumerator SetUpDI()
    {
        while (MapRefs.Instance == null)
            yield return null;

        var map = MapRefs.Instance;
        var mapGrid = map.GetComponentInParent<Grid>();

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

    /// <summary>
    /// Nếu player đang có buff MossTrap thì đặt bẫy rêu thay vì bomb thường.
    /// </summary>
    private bool TryPlaceMossTrap(Vector3 bombPosition)
    {
        if (!TryGetActiveEffectTemplate(EffectType.MossTrap, out EffectTemplate effect))
            return false;

        if (effect.placedBombPrefab == null)
        {
            Debug.LogWarning("MossTrap effect does not have placedBombPrefab.");
            return false;
        }

        GameObject trapObject = Instantiate(
            effect.placedBombPrefab,
            bombPosition,
            Quaternion.identity,
            bombParent
        );

        networkManager.Spawn(trapObject);

        MossTrapController trap = trapObject.GetComponent<MossTrapController>();

        if (trap != null)
            trap.Init(this, effect);

        return true;
    }

    /// <summary>
    /// Lấy EffectTemplate đang active theo EffectType.
    /// Server dùng hàm này để quyết định đặt bomb thường hay đặt trap.
    /// </summary>
    private bool TryGetActiveEffectTemplate(EffectType effectType, out EffectTemplate template)
    {
        template = null;

        if (playerEffects == null)
            return false;

        return playerEffects.TryGetActiveEffectTemplate(effectType, out template);
    }

}
