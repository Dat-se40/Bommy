using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Effect")]
public class EffectTemplate : ScriptableObject
{
    public int effectId;

    public string displayName;

    public EffectType effectType;

    public float duration;

    public float specialValue;

    public string description; 

    [Range(0f, 1f)]
    public float rating;

    public GameObject pickupPrefab;

    public GameObject vfxPrefab;
}