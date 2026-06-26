using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều khiển SoundSettingsOverlay — SFX và BGM tách riêng qua SoundManager (thang 0–100%).
/// </summary>
public class SoundSettingsDialogController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField]
    private GameObject dialogRoot;

    [Header("Buttons")]
    [SerializeField]
    private Button closeSoundSettingsbtn;

    [Header("Audio UI")]
    [SerializeField]
    private Slider sfxSlider;

    [SerializeField]
    private Slider bgmSlider;

    [SerializeField]
    private ToggleSwitch muteAllToggle;

    [SerializeField]
    private TMP_Text sfxValuelbl;

    [SerializeField]
    private TMP_Text bgmValuelbl;

    int workingSfxPercent;
    int workingBgmPercent;
    bool workingMuteAll;

    void Awake()
    {
        BindButtons();
        LoadSavedSettings();
        ApplyToUI();
        SyncAllToSoundManager();

        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    void BindButtons()
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
        SyncAllToSoundManager();

        if (dialogRoot != null)
        {
            dialogRoot.SetActive(true);
            dialogRoot.transform.SetAsLastSibling();
        }

        SoundManager.Instance?.PlayOpenDialog();
    }

    public void CloseDialog()
    {
        if (dialogRoot != null)
            dialogRoot.SetActive(false);
    }

    void LoadSavedSettings()
    {
        SoundManager manager = SoundManager.Instance;

        if (manager != null)
        {
            manager.LoadVolumeSettings();
            workingSfxPercent = manager.SfxVolumePercent;
            workingBgmPercent = manager.BgmVolumePercent;
            workingMuteAll = manager.IsMuteAll;
            return;
        }

        workingSfxPercent = 70;
        workingBgmPercent = 60;
        workingMuteAll = false;
    }

    void OnSfxSliderChanged(float value)
    {
        workingSfxPercent = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
        RefreshValueLabels();
        SoundManager.Instance?.SetSfxVolumePercent(workingSfxPercent);
    }

    void OnBgmSliderChanged(float value)
    {
        workingBgmPercent = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
        RefreshValueLabels();
        SoundManager.Instance?.SetBgmVolumePercent(workingBgmPercent);
    }

    void OnMuteAllChanged(bool value)
    {
        workingMuteAll = value;
        ApplyToUI();
        SoundManager.Instance?.SetMuteAll(workingMuteAll);
    }

    void ApplyToUI()
    {
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(workingSfxPercent / 100f);
            sfxSlider.interactable = !workingMuteAll;
        }

        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(workingBgmPercent / 100f);
            bgmSlider.interactable = !workingMuteAll;
        }

        if (muteAllToggle != null)
            muteAllToggle.SetStateWithoutNotify(workingMuteAll);

        RefreshValueLabels();
    }

    void RefreshValueLabels()
    {
        if (sfxValuelbl != null)
            sfxValuelbl.text = workingSfxPercent.ToString();

        if (bgmValuelbl != null)
            bgmValuelbl.text = workingBgmPercent.ToString();
    }

    void SyncAllToSoundManager()
    {
        SoundManager manager = SoundManager.Instance;

        if (manager == null)
            return;

        manager.SetSfxVolumePercent(workingSfxPercent);
        manager.SetBgmVolumePercent(workingBgmPercent);
        manager.SetMuteAll(workingMuteAll);
    }
}
