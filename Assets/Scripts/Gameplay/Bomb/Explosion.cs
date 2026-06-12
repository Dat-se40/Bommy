using PurrNet;
using UnityEngine;

public class Explosion : NetworkBehaviour
{
    [SerializeField] private float lifeTime = 0.35f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
