using System;
using UnityEngine;

[Serializable]
public struct LevelDropSettings
{
    [Range(0f, 1f)]
    public float dropChance;

    public DropEntry[] entries;

    public bool TryRoll(out EffectTemplate effect)
    {
        effect = null;

        if (dropChance <= 0f || entries == null || entries.Length == 0)
            return false;

        if (UnityEngine.Random.value > dropChance)
            return false;

        float totalWeight = 0f;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].effect != null)
                totalWeight += entries[i].weight;
        }

        if (totalWeight <= 0f)
            return false;

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].effect == null)
                continue;

            cumulative += entries[i].weight;

            if (roll <= cumulative)
            {
                effect = entries[i].effect;
                return true;
            }
        }

        return false;
    }
}
