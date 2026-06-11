using PurrNet;
using System.Collections;
using UnityEngine;

public class BombController : NetworkBehaviour
{
    [Header("Bomb Settings")]
    [SerializeField] private float fuseTime = 2f;

    [Header("References")]
    [SerializeField] private BoxCollider2D boxCollider;

    private PlayerController owner;
    private ExplosionCreator explosionCreator;
    private Vector3Int bombCell;
    private bool exploded;

    private void Awake()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();

        if (boxCollider != null)
            boxCollider.isTrigger = true;
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

        if (boxCollider != null)
            boxCollider.isTrigger = false;
    }
}
