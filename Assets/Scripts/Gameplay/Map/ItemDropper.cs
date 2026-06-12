using PurrNet;
using UnityEngine;

public class ItemDropper : MonoBehaviour
{
    [SerializeField]
    NetworkManager networkManager;

    [SerializeField]
    LevelConfig defaultLevelConfig;

    public void TryDropAt(Vector3 worldPosition)
    {
        LevelConfig config = LevelRuntime.Current != null
            ? LevelRuntime.Current
            : defaultLevelConfig;

        if (config == null)
        {
            Debug.LogWarning($"{nameof(ItemDropper)}: Không có {nameof(LevelConfig)}.");
            return;
        }

        if (!config.destructibleDrops.TryRoll(out EffectTemplate template))
            return;

        SpawnPickup(template, worldPosition);
    }

    void SpawnPickup(EffectTemplate template, Vector3 worldPosition)
    {
        if (networkManager == null)
        {
            Debug.LogWarning($"{nameof(ItemDropper)}: {nameof(NetworkManager)} chưa gán.");
            return;
        }

        PickableBuff pickable = PickableBuff.Spawn(template, worldPosition);

        if (pickable == null)
            return;

        networkManager.Spawn(pickable.gameObject);
    }

    void Awake()
    {
        if (networkManager == null)
            networkManager = FindFirstObjectByType<NetworkManager>();

        if (LevelRuntime.Current == null && defaultLevelConfig != null)
            LevelRuntime.SetLevel(defaultLevelConfig);
    }
}
