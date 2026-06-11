using UnityEngine;

[CreateAssetMenu(menuName = "Sound/Sound Data")]
public class SoundData : ScriptableObject
{
    public string key;

    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;
}
