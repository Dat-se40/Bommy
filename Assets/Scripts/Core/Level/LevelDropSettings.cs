using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct LevelDropSettings
{
    [Range(0f, 1f)]
    public float dropChance;

    public DropEntry[] entries;

    public bool TryRoll(out EffectTemplate effect)
    {
        return TryRoll(null, out effect);
    }

    public bool TryRoll(IReadOnlyDictionary<int, int> spawnedCounts, out EffectTemplate effect)
    {
        effect = null;

        if (dropChance <= 0f || entries == null || entries.Length == 0)
            return false;

        if (UnityEngine.Random.value > dropChance)
            return false;

        float totalWeight = 0f;

        for (int i = 0; i < entries.Length; i++)
        {
            if (IsEligibleEntry(entries[i], spawnedCounts))
                totalWeight += entries[i].weight;
        }

        if (totalWeight <= 0f)
            return false;

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < entries.Length; i++)
        {
            DropEntry entry = entries[i];

            if (!IsEligibleEntry(entry, spawnedCounts))
                continue;

            cumulative += entry.weight;

            if (roll <= cumulative)
            {
                effect = entry.effect;
                return true;
            }
        }

        return false;
    }

    static bool IsEligibleEntry(DropEntry entry, IReadOnlyDictionary<int, int> spawnedCounts)
    {
        if (entry.effect == null || entry.weight <= 0f)
            return false;

        if (entry.maxSpawnCount <= 0)
            return true;

        if (spawnedCounts == null)
            return true;

        int spawned = spawnedCounts.TryGetValue(entry.effect.effectId, out int count) ? count : 0;

        return spawned < entry.maxSpawnCount;
    }
}
