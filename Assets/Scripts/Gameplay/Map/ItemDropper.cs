//using UnityEngine;

//public class ItemDropper : MonoBehaviour
//{
//    [Header("Drop Prefabs")]
//    [SerializeField] private GameObject goldPrefab;
//    [SerializeField] private GameObject heartPrefab;
//    [SerializeField] private GameObject lifePrefab;

//    [Header("Drop Chance")]
//    [Range(0f, 1f)]
//    [SerializeField] private float dropChance = 0.35f;

//    [Range(0f, 1f)]
//    [SerializeField] private float goldChance = 0.65f;

//    [Range(0f, 1f)]
//    [SerializeField] private float heartChance = 0.25f;

//    public void TryDropItem(Vector3 position)
//    {
//        if (Random.value > dropChance)
//            return;

//        float roll = Random.value;

//        if (roll <= goldChance)
//        {
//            Spawn(goldPrefab, position);
//            return;
//        }

//        if (roll <= goldChance + heartChance)
//        {
//            Spawn(heartPrefab, position);
//            return;
//        }

//        Spawn(lifePrefab, position);
//    }

//    private void Spawn(GameObject prefab, Vector3 position)
//    {
//        if (prefab == null)
//            return;

//        Instantiate(prefab, position, Quaternion.identity);
//    }
//}
