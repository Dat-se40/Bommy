using PurrNet;
using UnityEngine;

public class DestroyAfterSeconds : NetworkBehaviour
{
    [SerializeField] private float seconds = 0.5f;

    private void Start()
    {
        Destroy(gameObject, seconds);
    }
}
