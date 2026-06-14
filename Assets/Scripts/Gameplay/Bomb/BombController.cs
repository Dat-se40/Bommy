using PurrNet;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class BombController : NetworkBehaviour
{
    [Header("Bomb Settings")]
    [SerializeField] private float fuseTime = 2f;

    [Header("References")]
    [SerializeField] private CircleCollider2D collider2D;

    private PlayerController owner;
    private ExplosionCreator explosionCreator;
    private Vector3Int bombCell;
    private bool exploded;

    private void Awake()
    {
        if (collider2D == null)
            collider2D = GetComponent<CircleCollider2D>();

        if (collider2D != null)
            collider2D.isTrigger = true;
    }
    
    public void Init(PlayerController owner, ExplosionCreator explosionCreator, Vector3Int bombCell)
    {
        this.owner = owner;
        this.explosionCreator = explosionCreator;
        this.bombCell = bombCell;

        StartCoroutine(ExplodeAfterDelay());
    }

    private IEnumerator ExplodeAfterDelay()
    {
        yield return new WaitForSeconds(fuseTime);
        Explode();
    }

    private void Explode()
    {
        if (exploded)
            return;

        exploded = true;

        if (!isServer)
            return;

        if (explosionCreator != null)
            explosionCreator.CreateExplosionAtCell(bombCell);

        if (owner != null)
            owner.OnBombExploded();

        Destroy(gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (collider2D != null) 
        {
            Debug.Log("[BOMB] Player goes out bomb range");
            collider2D.isTrigger = false;
        }
            

    }
}
