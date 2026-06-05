using PurrNet;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// BombController.cs
public class BombController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject bombPrefab;

    [Header("Bomb Settings")]
    [SerializeField] private float bombFuseTime = 3f;      // Thời gian nổ
    [SerializeField] private int bombAmount = 3;           // Số bomb tối đa
    [SerializeField] private float bombChargeTime = 3f;    // Thời gian hồi 1 bomb

    // Runtime
    private int bombRemaining;
    private float bombCurrentChargeTime;
    private MovementController movementController;
    private bool isReady;

    #region Network Lifecycle

    protected override void OnSpawned()
    {
        base.OnSpawned();

        // Khởi tạo sau khi object đã được spawn hoàn chỉnh
        bombRemaining = bombAmount;
        bombCurrentChargeTime = 0f;
        movementController = GetComponent<MovementController>();

        // Đăng ký event nếu ExplosionCreator tồn tại
        if (ExplosionCreator.Instance != null)
        {
            ExplosionCreator.Instance.onAllCompleted += OnExplosionComplete;
        }

        isReady = true;

        Debug.Log(
            $"BombController Spawned | " +
            $"Owner={isOwner} | " +
            $"Server={isServer} | " 
           // $"Observer={isObserver}"
        );
    }

    protected override void OnDespawned()
    {
        base.OnDespawned();

        if (ExplosionCreator.Instance != null)
        {
            ExplosionCreator.Instance.onAllCompleted -= OnExplosionComplete;
        }

        isReady = false;
    }

    #endregion

    private void Update()
    {
        // Chỉ owner mới được nhận input
        if (!isReady) return;
        if (!isOwner) return;

        // Nếu chưa lấy được MovementController thì thử lại
        if (movementController == null)
        {
            movementController = GetComponent<MovementController>();
            if (movementController == null) return;
        }

        RechargeBomb();

        // Chỉ đặt bomb khi:
        // - Nhấn Space
        // - Còn bomb
        // - Nhân vật đang đứng yên
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame &&
            bombRemaining > 0 &&
            !movementController.isMoving)
        {
            bombRemaining--;

            // Gọi ServerRpc để server xử lý việc đặt bomb
            CmdPlaceBomb();
        }
    }

    /// <summary>
    /// Hồi lại bomb theo thời gian.
    /// </summary>
    private void RechargeBomb()
    {
        if (bombRemaining >= bombAmount)
            return;

        bombCurrentChargeTime += Time.deltaTime;

        if (bombCurrentChargeTime >= bombChargeTime)
        {
            bombCurrentChargeTime = 0f;
            bombRemaining = Mathf.Min(bombRemaining + 1, bombAmount);

            Debug.Log($"Bomb recharged. Remaining: {bombRemaining}");
        }
    }

    /// <summary>
    /// Client gửi yêu cầu lên server để đặt bomb.
    /// </summary>
    [ServerRpc]
    private void CmdPlaceBomb(RPCInfo rpcInfo = default)
    {
        Debug.Log($"Player {rpcInfo.sender.id} placed a bomb");

        // Snap vị trí bomb vào tâm ô lưới
        Vector2 bombPosition = new Vector2(
            Mathf.Floor(transform.position.x) + 0.5f,
            Mathf.Floor(transform.position.y) + 0.5f
        );

        StartCoroutine(ServerPlaceBombRoutine(bombPosition));
    }

    /// <summary>
    /// Server tạo bomb, đợi fuse time rồi kích hoạt explosion.
    /// </summary>
    private IEnumerator ServerPlaceBombRoutine(Vector2 position)
    {
        // Tạo bomb object (chỉ để hiển thị)
        GameObject bomb = null;

        if (bombPrefab != null)
        {
            bomb = Instantiate(bombPrefab, position, Quaternion.identity);
        }

        // Chờ bomb phát nổ
        yield return new WaitForSeconds(bombFuseTime/2);
        bomb.GetComponent<CircleCollider2D>().isTrigger = false;
        yield return new WaitForSeconds(bombFuseTime / 2);
        // Xóa bomb visual
        if (bomb != null)
        {
            Destroy(bomb);
        }

        // Gây nổ tại server
        StartExplosion(position);
    }

    /// <summary>
    /// Kích hoạt explosion theo 4 hướng.
    /// ExplosionCreator đã tự kiểm tra isServer.
    /// </summary>
    private void StartExplosion(Vector2 position)
    {
        if (ExplosionCreator.Instance == null)
        {
            Debug.LogError("ExplosionCreator.Instance is null!");
            return;
        }

        // Tạo vụ nổ trung tâm (nếu muốn)
        // ExplosionCreator.Instance.CreateOnDirection(position, Vector2.zero);

        // 4 hướng
        ExplosionCreator.Instance.CreateOnDirection(position, Vector2.up);
        ExplosionCreator.Instance.CreateOnDirection(position, Vector2.down);
        ExplosionCreator.Instance.CreateOnDirection(position, Vector2.left);
        ExplosionCreator.Instance.CreateOnDirection(position, Vector2.right);

        Debug.Log($"Explosion started at {position}");
    }

    /// <summary>
    /// Callback khi toàn bộ explosion đã hoàn thành.
    /// </summary>
    private void OnExplosionComplete()
    {
        Debug.Log("All explosion visuals completed.");
    }

    #region Public Helpers (Optional)

    public int GetRemainingBombs()
    {
        return bombRemaining;
    }

    public bool CanPlaceBomb()
    {
        return isReady &&
               isOwner &&
               bombRemaining > 0 &&
               movementController != null &&
               !movementController.isMoving;
    }

    #endregion
}