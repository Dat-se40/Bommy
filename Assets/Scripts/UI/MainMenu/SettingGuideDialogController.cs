using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Điều khiển dialog Setting/Guide ở MainMenu.
/// Setting chỉ chỉnh âm lượng SFX/BGM và Mute All.
/// </summary>
public class SettingGuideDialogController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject dialogRoot;
    [SerializeField] private GameObject settingPage;
    [SerializeField] private GameObject guidePage;

    [Header("Open / Close")]
    [SerializeField] private Button openbtn;
    [SerializeField] private Button closebtn;

    [Header("Tabs")]
    [SerializeField] private Button settingTabbtn;
    [SerializeField] private Button guideTabbtn;

    [Header("Audio UI")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private ToggleSwitch muteAllToggle;

    [SerializeField] private TMP_Text sfxValuelbl;
    [SerializeField] private TMP_Text bgmValuelbl;

    [Header("Bottom Buttons")]
    [SerializeField] private Button resetbtn;
    [SerializeField] private Button savebtn;

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

        ShowSettingTab();

        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    private void BindButtons()
    {
        if (openbtn != null)
            openbtn.onClick.AddListener(OpenDialog);

        if (closebtn != null)
            closebtn.onClick.AddListener(CloseDialog);

        if (settingTabbtn != null)
            settingTabbtn.onClick.AddListener(ShowSettingTab);

        if (guideTabbtn != null)
            guideTabbtn.onClick.AddListener(ShowGuideTab);

        if (resetbtn != null)
            resetbtn.onClick.AddListener(ResetWorkingSettings);

        if (savebtn != null)
            savebtn.onClick.AddListener(SaveSettings);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);

        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);

        if (muteAllToggle != null)
            muteAllToggle.OnValueChanged.AddListener(OnMuteAllChanged);
    }

    public void OpenDialog()
    {
        LoadSavedSettings();
        ApplyToUI();
        ApplyAudio();

        if (dialogRoot != null)
            dialogRoot.SetActive(true);

        ShowSettingTab();
    }

    public void CloseDialog()
    {
        // Đóng bằng X sẽ hủy thay đổi chưa Save và quay về setting đã lưu.
        LoadSavedSettings();
        ApplyToUI();
        ApplyAudio();

        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    private void ShowSettingTab()
    {
        if (settingPage != null)
            settingPage.SetActive(true);

        if (guidePage != null)
            guidePage.SetActive(false);
    }

    private void ShowGuideTab()
    {
        if (settingPage != null)
            settingPage.SetActive(false);

        if (guidePage != null)
            guidePage.SetActive(true);
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

        ApplyAudio();
    }

    private void ResetWorkingSettings()
    {
        workingSfx = DefaultSfx;
        workingBgm = DefaultBgm;
        workingMuteAll = false;

        ApplyToUI();
        ApplyAudio();
    }

    private void OnSfxSliderChanged(float value)
    {
        workingSfx = value;
        RefreshValueLabels();
        ApplyAudio();
    }

    private void OnBgmSliderChanged(float value)
    {
        workingBgm = value;
        RefreshValueLabels();
        ApplyAudio();
    }

    private void OnMuteAllChanged(bool value)
    {
        workingMuteAll = value;
        ApplyToUI();
        ApplyAudio();
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
