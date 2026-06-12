using UnityEngine;

public class Explosion : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.35f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
