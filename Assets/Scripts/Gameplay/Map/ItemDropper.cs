using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class ItemDropper : MonoBehaviour
{
    [SerializeField]
    NetworkManager networkManager;

    [SerializeField]
    LevelConfig defaultLevelConfig;

    readonly Dictionary<int, int> spawnedCounts = new();

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

        if (!config.destructibleDrops.TryRoll(spawnedCounts, out EffectTemplate template))
            return;

        RegisterSpawn(template);
        SpawnPickup(template, worldPosition);
    }

    void RegisterSpawn(EffectTemplate template)
    {
        if (template == null)
            return;

        spawnedCounts.TryGetValue(template.effectId, out int count);
        spawnedCounts[template.effectId] = count + 1;
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

        spawnedCounts.Clear();
    }
}
