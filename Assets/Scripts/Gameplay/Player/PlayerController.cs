using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Bomb")]
    [SerializeField] private BombController bombPrefab;
    [SerializeField] private ExplosionCreator explosionCreator;
    [SerializeField] private Transform bombParent;
    [SerializeField] private LayerMask bombLayer;

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
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
            PlaceBomb();
    }

    private void PlaceBomb()
    {
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

        Vector3Int bombCell = movementController.CurrentCell;
        Vector3 bombPosition = grid.GetCellCenterWorld(bombCell);

        bool alreadyHasBomb = Physics2D.OverlapBox(
            bombPosition,
            new Vector2(0.75f, 0.75f),
            0f,
            bombLayer
        );

        if (alreadyHasBomb)
            return;

        BombController bomb = Instantiate(
            bombPrefab,
            bombPosition,
            Quaternion.identity,
            bombParent
        );

        bomb.Init(this, explosionCreator, bombCell);

        activeBombs++;
    }

    public void OnBombExploded()
    {
        activeBombs--;

        if (activeBombs < 0)
            activeBombs = 0;
    }
}
