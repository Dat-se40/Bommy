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

    private int activeBombs;

    private void Awake()
    {
        if (movementController == null)
            movementController = GetComponent<MovementController>();

        if (playerInfor == null)
            playerInfor = GetComponent<PlayerInfor>();
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
        // Cách để đợi các DI đã xong
        StartCoroutine(SetUpDI());
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
}
