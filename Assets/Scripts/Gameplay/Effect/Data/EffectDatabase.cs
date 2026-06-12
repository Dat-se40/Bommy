using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EffectDatabase : MonoBehaviour
{
    public static EffectDatabase Instance;

    [SerializeField]
    List<EffectTemplate> templates;

    Dictionary<int, EffectTemplate> lookup;

    void Awake()
    {
        Instance = this;

        lookup = templates.ToDictionary(
            t => t.effectId,
            t => t);
    }

    public EffectTemplate Get(int id)
    {
        return lookup[id];
    }
}