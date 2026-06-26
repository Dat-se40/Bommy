using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    const string PrefsSfxVolumePercent = "Audio_SfxVolumePercent";
    const string PrefsBgmVolumePercent = "Audio_BgmVolumePercent";
    const string PrefsMuteAll = "Audio_MuteAll";
    const string PrefsSfxMuted = "Audio_SfxMuted";
    const string PrefsBgmMuted = "Audio_BgmMuted";

    [SerializeField]
    SoundLibrary defaultLibrary;

    [Header("Audio Mixer Groups (route only — volume điều khiển trên AudioSource)")]
    [SerializeField]
    AudioMixerGroup sfxMixerGroup;

    [SerializeField]
    AudioMixerGroup bgmMixerGroup;

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

    [Header("Volume Defaults (0–100%)")]
    [SerializeField]
    int defaultSfxVolumePercent = 70;

    [SerializeField]
    int defaultBgmVolumePercent = 60;

    SoundLibrary sceneLibrary;
    string currentBgmKey;
    float currentBgmClipVolume = 1f;

    int sfxVolumePercent;
    int bgmVolumePercent;
    bool muteAll;
    bool sfxMuted;
    bool bgmMuted;

    public AudioSource SfxSource => sfxSource;
    public AudioSource BgmSource => bgmSource;

    public int SfxVolumePercent => sfxVolumePercent;
    public int BgmVolumePercent => bgmVolumePercent;
    public bool IsMuteAll => muteAll;
    public bool IsSfxMuted => muteAll || sfxMuted;
    public bool IsBgmMuted => muteAll || bgmMuted;

    float SfxVolumeScalar => IsSfxMuted ? 0f : sfxVolumePercent / 100f;
    float BgmVolumeScalar => IsBgmMuted ? 0f : bgmVolumePercent / 100f;

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
        LoadVolumeSettings();
        ApplyBgmSourceVolume();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void EnsureAudioSources()
    {
        TryAssignSourcesOnSameObject();
        EnsureSfxSource();
        EnsureBgmSource();
    }

    void TryAssignSourcesOnSameObject()
    {
        if (sfxSource != null || bgmSource != null)
            return;

        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length < 2)
            return;

        sfxSource = sources[0];
        bgmSource = sources[1];
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        sfxSource.playOnAwake = false;
    }

    void EnsureSfxSource()
    {
        if (sfxSource == null)
            sfxSource = FindChildSource("SFX");

        if (sfxSource == null)
        {
            GameObject sfxGo = new("SFX");
            sfxGo.transform.SetParent(transform, false);
            sfxSource = sfxGo.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        sfxSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    void EnsureBgmSource()
    {
        if (bgmSource == null)
            bgmSource = FindChildSource("BGM");

        if (bgmSource == null)
        {
            GameObject bgmGo = new("BGM");
            bgmGo.transform.SetParent(transform, false);
            bgmSource = bgmGo.AddComponent<AudioSource>();
        }

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
    }

    AudioSource FindChildSource(string childName)
    {
        Transform child = transform.Find(childName);

        return child != null ? child.GetComponent<AudioSource>() : null;
    }

    public void LoadVolumeSettings()
    {
        MigrateLegacyVolumePrefs();

        sfxVolumePercent = PlayerPrefs.GetInt(PrefsSfxVolumePercent, defaultSfxVolumePercent);
        bgmVolumePercent = PlayerPrefs.GetInt(PrefsBgmVolumePercent, defaultBgmVolumePercent);
        muteAll = PlayerPrefs.GetInt(PrefsMuteAll, 0) == 1;
        sfxMuted = PlayerPrefs.GetInt(PrefsSfxMuted, 0) == 1;
        bgmMuted = PlayerPrefs.GetInt(PrefsBgmMuted, 0) == 1;

        sfxVolumePercent = Mathf.Clamp(sfxVolumePercent, 0, 100);
        bgmVolumePercent = Mathf.Clamp(bgmVolumePercent, 0, 100);
    }

    static void MigrateLegacyVolumePrefs()
    {
        const string legacySfx = "Audio_SfxVolume";
        const string legacyBgm = "Audio_BgmVolume";

        if (!PlayerPrefs.HasKey(PrefsSfxVolumePercent) && PlayerPrefs.HasKey(legacySfx))
        {
            int percent = Mathf.RoundToInt(PlayerPrefs.GetFloat(legacySfx, 0.7f) * 100f);
            PlayerPrefs.SetInt(PrefsSfxVolumePercent, Mathf.Clamp(percent, 0, 100));
        }

        if (!PlayerPrefs.HasKey(PrefsBgmVolumePercent) && PlayerPrefs.HasKey(legacyBgm))
        {
            int percent = Mathf.RoundToInt(PlayerPrefs.GetFloat(legacyBgm, 0.6f) * 100f);
            PlayerPrefs.SetInt(PrefsBgmVolumePercent, Mathf.Clamp(percent, 0, 100));
        }
    }

    public void SaveVolumeSettings()
    {
        PlayerPrefs.SetInt(PrefsSfxVolumePercent, sfxVolumePercent);
        PlayerPrefs.SetInt(PrefsBgmVolumePercent, bgmVolumePercent);
        PlayerPrefs.SetInt(PrefsMuteAll, muteAll ? 1 : 0);
        PlayerPrefs.SetInt(PrefsSfxMuted, sfxMuted ? 1 : 0);
        PlayerPrefs.SetInt(PrefsBgmMuted, bgmMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSfxVolumePercent(int percent)
    {
        sfxVolumePercent = Mathf.Clamp(percent, 0, 100);
        SaveVolumeSettings();
    }

    public void SetBgmVolumePercent(int percent)
    {
        bgmVolumePercent = Mathf.Clamp(percent, 0, 100);
        ApplyBgmSourceVolume();
        SaveVolumeSettings();
    }

    public void SetSfxMuted(bool muted)
    {
        sfxMuted = muted;
        SaveVolumeSettings();
    }

    public void SetBgmMuted(bool muted)
    {
        bgmMuted = muted;
        RefreshBgmPlayback();
        SaveVolumeSettings();
    }

    public void SetMuteAll(bool muted)
    {
        muteAll = muted;
        RefreshBgmPlayback();
        SaveVolumeSettings();
    }

    public void SetBgmVolume(float volume01)
    {
        SetBgmVolumePercent(Mathf.RoundToInt(Mathf.Clamp01(volume01) * 100f));
    }

    void ApplyBgmSourceVolume()
    {
        if (bgmSource == null)
            return;

        bgmSource.volume = currentBgmClipVolume * BgmVolumeScalar;
    }

    void RefreshBgmPlayback()
    {
        ApplyBgmSourceVolume();

        if (bgmSource == null || string.IsNullOrEmpty(currentBgmKey) || bgmSource.clip == null)
            return;

        if (IsBgmMuted)
            bgmSource.Stop();
        else if (!bgmSource.isPlaying)
            bgmSource.Play();
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
        if (IsSfxMuted)
            return;

        if (!TryGetSound(key, out SoundData data) || data.clip == null)
        {
            Debug.LogWarning($"{nameof(SoundManager)}: Không tìm thấy SFX '{key}'.");
            return;
        }

        float volume = data.volume * SfxVolumeScalar;

        if (volume <= 0f)
            return;

        sfxSource.pitch = Random.Range(sfxPitchMin, sfxPitchMax);
        sfxSource.PlayOneShot(data.clip, volume);
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
        {
            ApplyBgmSourceVolume();
            return;
        }

        currentBgmKey = key;
        currentBgmClipVolume = data.volume;
        bgmSource.clip = data.clip;
        bgmSource.pitch = 1f;
        bgmSource.loop = true;
        ApplyBgmSourceVolume();

        if (!IsBgmMuted)
            bgmSource.Play();
        else
            bgmSource.Stop();
    }

    public void StopBgm()
    {
        currentBgmKey = null;
        currentBgmClipVolume = 1f;
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void PlayOpenDialog()
    {
        PlaySfx(SoundKey.SfxOpenDialog);
    }

    internal void StopAllSfx()
    {
        sfxSource.Stop();
        sfxSource.clip = null; 
    }
}
