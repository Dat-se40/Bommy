using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField]
    SoundLibrary defaultLibrary;

    [Header("SFX")]
    [SerializeField]
    AudioSource sfxSource;

    [SerializeField]
    float sfxPitchMin = 0.92f;

    [SerializeField]
    float sfxPitchMax = 1.08f;

    [Header("BGM")]
    [SerializeField]
    AudioSource bgmSource;

    SoundLibrary sceneLibrary;
    string currentBgmKey;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void EnsureAudioSources()
    {
        if (sfxSource == null)
        {
            var sfxGo = new GameObject("SFX");
            sfxGo.transform.SetParent(transform);
            sfxSource = sfxGo.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (bgmSource == null)
        {
            var bgmGo = new GameObject("BGM");
            bgmGo.transform.SetParent(transform);
            bgmSource = bgmGo.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
        }
    }

    public void SetSceneLibrary(SoundLibrary library)
    {
        sceneLibrary = library;
    }

    public void ClearSceneLibrary()
    {
        sceneLibrary = null;
    }

    public bool TryGetSound(string key, out SoundData data)
    {
        if (sceneLibrary != null && sceneLibrary.TryGet(key, out data))
            return true;

        if (defaultLibrary != null && defaultLibrary.TryGet(key, out data))
            return true;

        data = null;
        return false;
    }

    public SoundData GetSound(string key)
    {
        return TryGetSound(key, out SoundData data) ? data : null;
    }

    public void PlaySfx(string key)
    {
        if (!TryGetSound(key, out SoundData data) || data.clip == null)
        {
            Debug.LogWarning($"{nameof(SoundManager)}: Không tìm thấy SFX '{key}'.");
            return;
        }

        sfxSource.pitch = Random.Range(sfxPitchMin, sfxPitchMax);
        sfxSource.PlayOneShot(data.clip, data.volume);
        sfxSource.pitch = 1f;
    }

    public void PlayBgm(string key, bool restartIfSame = false)
    {
        if (!TryGetSound(key, out SoundData data) || data.clip == null)
        {
            Debug.LogWarning($"{nameof(SoundManager)}: Không tìm thấy BGM '{key}'.");
            return;
        }

        if (!restartIfSame &&
            currentBgmKey == key &&
            bgmSource.isPlaying &&
            bgmSource.clip == data.clip)
            return;

        currentBgmKey = key;
        bgmSource.clip = data.clip;
        bgmSource.volume = data.volume;
        bgmSource.pitch = 1f;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        currentBgmKey = null;
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void SetBgmVolume(float volume)
    {
        bgmSource.volume = Mathf.Clamp01(volume);
    }
}
