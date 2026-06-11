using System;
using UnityEngine;

[Serializable]
public struct DropEntry
{
    public EffectTemplate effect;

    [Range(0f, 1f)]
    public float weight;
}
