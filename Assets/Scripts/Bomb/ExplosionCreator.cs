using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ExplosionCreator : MonoBehaviour
{
    [SerializeField] GameObject explosionPrefab;
    [SerializeField] int explosionRange = 5;

    public event Action eventComplete;

    private List<Explosion> activeExplosions = new();
    private int numberExploded = 0;
    // BombController chỉ cần gọi cái này
    public void CreateOnDirection(Vector2 bombPosition, Vector2 direction)
    {
        StartCoroutine(SpawnAlongDirection(bombPosition, direction));
    }

    private IEnumerator SpawnAlongDirection(Vector2 startPosition, Vector2 direction)
    {
        var nextPosition = startPosition;

        for (int i = 0; i < explosionRange; i++)
        {
            nextPosition += direction;

            var exp = Instantiate(explosionPrefab, nextPosition, Quaternion.identity)
                          .GetComponent<Explosion>();
            exp.transform.rotation = DirectionToRotation(direction);
            exp.explodedEvent += OnExplode;
            activeExplosions.Add(exp);

            bool blocked = IsBlocked(nextPosition);
           // yield return new WaitForSeconds(exp.clipLength);
            yield return null;
            if (blocked) yield break;
        }
    }

    private bool IsBlocked(Vector2 position)
    {
        Collider2D col = Physics2D.OverlapCircle(position, 0.1f);
        if (col == null) return false;

        int layer = col.gameObject.layer;
        return layer == LayerMask.NameToLayer("Block") ||
               layer == LayerMask.NameToLayer("Indestructibles") ||
               layer == LayerMask.NameToLayer("Destructibles");
    }

    private Quaternion DirectionToRotation(Vector2 direction)
    {
        if (direction == Vector2.right) return Quaternion.Euler(0, 0, 0);
        if (direction == Vector2.left) return Quaternion.Euler(0, 0, 180f);
        if (direction == Vector2.up) return Quaternion.Euler(0, 0, 90f);
        if (direction == Vector2.down) return Quaternion.Euler(0, 0, -90f);
        return Quaternion.identity;
    }
    private void OnExplode()
    {
        numberExploded++;
//        if (!allSpawned) return; 
        if (numberExploded < activeExplosions.Count) return;

        // Tất cả explosion xong
        eventComplete?.Invoke();
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        foreach (var exp in activeExplosions) 
        {
            exp.explodedEvent -= OnExplode;
            Destroy(exp.gameObject);
        }
            
        activeExplosions.Clear();
        numberExploded = 0;
        // TODO: pool thật sự thì trả object về pool thay vì Destroy
    }
}