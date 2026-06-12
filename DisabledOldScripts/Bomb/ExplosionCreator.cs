using PurrNet;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionCreator : NetworkBehaviour, IHitableSkillCreator
{
    [SerializeField] GameObject explosionPrefab;
    [SerializeField] int explosionRange = 5;

    public event Action onAllCompleted;
    public static ExplosionCreator Instance { get; private set; }

    // Track số coroutine đang chạy để biết khi nào tất cả xong
    private int _activeRoutines = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Gọi từ BombController (server only).
    /// Tính các vị trí hợp lệ, xử lý hit logic, rồi spawn visual.
    /// </summary>
    public void CreateOnDirection(Vector2 position, Vector2 direction)
    {
        if (!isServer) return;

        List<Vector2> validPositions = new List<Vector2>();
        Vector2 next = position;

        for (int i = 0; i < explosionRange; i++)
        {
            next += direction;
            validPositions.Add(next);
            HandleServerSideHit(next);
            if (IsBlocked(next)) break;
        }

        if (validPositions.Count > 0)
        {
            _activeRoutines++;
            StartCoroutine(SpawnExplosionsRoutine(validPositions, direction));
        }
    }

    /// <summary>
    /// Server spawn từng explosion theo thứ tự, PurrNet tự sync sang client.
    /// Sau khi xong, kiểm tra có phải direction cuối không thì fire onAllCompleted.
    /// </summary>
    private IEnumerator SpawnExplosionsRoutine(List<Vector2> positions, Vector2 direction)
    {
        float clipLength = explosionPrefab.GetComponent<Explosion>()?.clipLength ?? 1f;

        foreach (Vector2 pos in positions)
        {
            // PurrNet: Instantiate trên server → tự sync sang tất cả client
            GameObject exp = Instantiate(explosionPrefab, pos, DirectionToRotation(direction));

            // Fallback destroy nếu Animation Event không fire
            Destroy(exp, clipLength + 0.5f);

            yield return new WaitForSeconds(0.05f);
        }

        // Đợi animation xong
        yield return new WaitForSeconds(clipLength);

        _activeRoutines--;
        if (_activeRoutines <= 0)
        {
            _activeRoutines = 0;
            onAllCompleted?.Invoke();
        }
    }

    /// <summary>
    /// Xử lý hit logic trên server: phá gạch, damage player, v.v.
    /// </summary>
    private void HandleServerSideHit(Vector2 position)
    {
        Collider2D col = Physics2D.OverlapCircle(position, 0.1f);
        if (col == null) return;
        int layer = col.gameObject.layer; 
        if (layer == LayerMask.NameToLayer("Indestructibles"))
        {
        //    Debug.Log("Hit " + gameObject.name);
        //    Destroy(col.gameObject); 
        } else if (layer == LayerMask.NameToLayer("Player"))
        {
           // Debug.Log($"Player {owner.Value.id} -> {col.gameObject.name}");
        }
        // TODO: xử lý player damage, phá destructible tiles, v.v.
    }

    private bool IsBlocked(Vector2 position)
    {
        Collider2D col = Physics2D.OverlapCircle(position, 0.1f);
        if (col == null) return false;

        int layer = col.gameObject.layer;
        return layer == LayerMask.NameToLayer("Block") ||
               layer == LayerMask.NameToLayer("Indestructibles") ||
               layer == LayerMask.NameToLayer("Destructibles") ||
               layer == LayerMask.NameToLayer("Bomb");
    }

    private Quaternion DirectionToRotation(Vector2 direction)
    {
        if (direction == Vector2.right) return Quaternion.Euler(0, 0, 0);
        if (direction == Vector2.left) return Quaternion.Euler(0, 0, 180f);
        if (direction == Vector2.up) return Quaternion.Euler(0, 0, 90f);
        if (direction == Vector2.down) return Quaternion.Euler(0, 0, -90f);
        return Quaternion.identity;
    }
}