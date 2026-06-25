using System;
using UnityEngine;

[Serializable]
public struct DropEntry
{
    public EffectTemplate effect;

    [Range(0f, 1f)]
    public float weight;

    /// <summary>0 = không giới hạn số lần spawn trên map trong một trận.</summary>
    [Min(0)]
    public int maxSpawnCount;
}
