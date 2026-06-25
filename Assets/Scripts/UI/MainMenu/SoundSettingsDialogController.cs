using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Điều khiển SoundSettingsOverlay.
/// </summary>
public class SoundSettingsDialogController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject dialogRoot;

    [Header("Buttons")]
    [SerializeField] private Button closeSoundSettingsbtn;

    [Header("Audio UI")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private ToggleSwitch muteAllToggle;
    [SerializeField] private TMP_Text sfxValuelbl;
    [SerializeField] private TMP_Text bgmValuelbl;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string sfxVolumeParam = "SfxVolume";
    [SerializeField] private string bgmVolumeParam = "BgmVolume";

    private const string SfxKey = "Audio_SfxVolume";
    private const string BgmKey = "Audio_BgmVolume";
    private const string MuteAllKey = "Audio_MuteAll";

    private const float DefaultSfx = 0.7f;
    private const float DefaultBgm = 0.6f;

    private float workingSfx;
    private float workingBgm;
    private bool workingMuteAll;

    private void Awake()
    {
        BindButtons();

        LoadSavedSettings();
        ApplyToUI();
        ApplyAudio();

        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    private void BindButtons()
    {
        if (closeSoundSettingsbtn != null)
        {
            closeSoundSettingsbtn.onClick.RemoveAllListeners();
            closeSoundSettingsbtn.onClick.AddListener(CloseDialog);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        }

        if (muteAllToggle != null)
        {
            muteAllToggle.OnValueChanged.RemoveAllListeners();
            muteAllToggle.OnValueChanged.AddListener(OnMuteAllChanged);
        }
    }

    public void OpenDialog()
    {
        LoadSavedSettings();
        ApplyToUI();
        ApplyAudio();

        if (dialogRoot != null)
        {
            dialogRoot.SetActive(true);
            dialogRoot.transform.SetAsLastSibling();
        }
    }

    public void CloseDialog()
    {
        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    private void LoadSavedSettings()
    {
        workingSfx = PlayerPrefs.GetFloat(SfxKey, DefaultSfx);
        workingBgm = PlayerPrefs.GetFloat(BgmKey, DefaultBgm);
        workingMuteAll = PlayerPrefs.GetInt(MuteAllKey, 0) == 1;
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(SfxKey, workingSfx);
        PlayerPrefs.SetFloat(BgmKey, workingBgm);
        PlayerPrefs.SetInt(MuteAllKey, workingMuteAll ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnSfxSliderChanged(float value)
    {
        workingSfx = value;
        RefreshValueLabels();
        ApplyAudio();
        SaveSettings();
    }

    private void OnBgmSliderChanged(float value)
    {
        workingBgm = value;
        RefreshValueLabels();
        ApplyAudio();
        SaveSettings();
    }

    private void OnMuteAllChanged(bool value)
    {
        workingMuteAll = value;
        ApplyToUI();
        ApplyAudio();
        SaveSettings();
    }

    private void ApplyToUI()
    {
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(workingSfx);
            sfxSlider.interactable = !workingMuteAll;
        }

        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(workingBgm);
            bgmSlider.interactable = !workingMuteAll;
        }

        if (muteAllToggle != null)
            muteAllToggle.SetStateWithoutNotify(workingMuteAll);

        RefreshValueLabels();
    }

    private void RefreshValueLabels()
    {
        if (sfxValuelbl != null)
            sfxValuelbl.text = Mathf.RoundToInt(workingSfx * 100f).ToString();

        if (bgmValuelbl != null)
            bgmValuelbl.text = Mathf.RoundToInt(workingBgm * 100f).ToString();
    }

    /// <summary>
    /// Áp âm lượng vào AudioMixer. Volume 0 được map thành -80 dB.
    /// </summary>
    private void ApplyAudio()
    {
        if (audioMixer == null)
            return;

        if (workingMuteAll)
        {
            audioMixer.SetFloat(sfxVolumeParam, -80f);
            audioMixer.SetFloat(bgmVolumeParam, -80f);
            return;
        }

        audioMixer.SetFloat(sfxVolumeParam, VolumeToDb(workingSfx));
        audioMixer.SetFloat(bgmVolumeParam, VolumeToDb(workingBgm));
    }

    private float VolumeToDb(float value)
    {
        if (value <= 0.0001f)
            return -80f;

        return Mathf.Log10(value) * 20f;
    }
}
