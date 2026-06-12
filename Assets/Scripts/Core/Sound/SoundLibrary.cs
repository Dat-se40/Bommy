using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Sound/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [SerializeField]
    List<SoundData> sounds = new();

    Dictionary<string, SoundData> lookup;

    void OnEnable()
    {
        BuildLookup();
    }

    void OnValidate()
    {
        BuildLookup();
    }

    void BuildLookup()
    {
        lookup ??= new Dictionary<string, SoundData>();
        lookup.Clear();

        foreach (SoundData sound in sounds)
        {
            if (sound == null || string.IsNullOrEmpty(sound.key))
                continue;

            lookup[sound.key] = sound;
        }
    }

    public bool TryGet(string key, out SoundData data)
    {
        if (lookup == null)
            BuildLookup();

        return lookup.TryGetValue(key, out data);
    }

    public SoundData Get(string key)
    {
        return TryGet(key, out SoundData data) ? data : null;
    }

    public bool Contains(string key)
    {
        if (lookup == null)
            BuildLookup();

        return lookup.ContainsKey(key);
    }
}
