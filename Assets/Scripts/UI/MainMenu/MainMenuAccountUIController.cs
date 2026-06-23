using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Điều khiển TopBar, SettingsOverlay và thông tin tài khoản ở MainMenu.
/// </summary>
public class MainMenuAccountUIController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string authGateSceneName = "AuthGate_demo";

    [Header("Top Bar")]
    [SerializeField] private Button settingbtn;
    [SerializeField] private Button mailboxbtn;

    [Header("Settings Overlay")]
    [SerializeField] private GameObject settingsOverlay;
    [SerializeField] private Button closeSettingsbtn;

    [Header("Account Info")]
    [SerializeField] private TMP_Text accountNamelbl;
    [SerializeField] private TMP_Text accountIdlbl;
    [SerializeField] private Button copyIdbtn;

    [Header("Edit Name")]
    [SerializeField] private Button editNamebtn;
    [SerializeField] private NameEditDialogController nameEditDialog;

    [Header("Setting Entries")]
    [SerializeField] private Button soundSettingsEntrybtn;
    [SerializeField] private Button bindAccountEntrybtn;
    [SerializeField] private Button changePasswordEntrybtn;
    [SerializeField] private Button redeemCodeEntrybtn;

    [Header("Bottom Buttons")]
    [SerializeField] private Button switchAccountbtn;
    [SerializeField] private Button logoutbtn;

    [Header("Dialogs")]
    [SerializeField] private SoundSettingsDialogController soundSettingsDialog;
    [SerializeField] private BindAccountDialogController bindAccountDialog;
    [SerializeField] private ChangePasswordDialogController changePasswordDialog;
    [SerializeField] private RedeemCodeDialogController redeemCodeDialog;

    [Header("Mailbox")]
    [SerializeField] private MailboxDialogController mailboxDialog;

    private const string SessionKey = "AUTH_SESSION_ACTIVE";
    private const string AccountEmailKey = "AUTH_ACCOUNT_EMAIL";
    private const string AccountIdKey = "AUTH_ACCOUNT_ID";
    private const string AccountNameKey = "AUTH_ACCOUNT_NAME";
    private const string AccountProviderKey = "AUTH_ACCOUNT_PROVIDER";

    private void Awake()
    {
        BindButtons();

        if (settingsOverlay != null)
            settingsOverlay.SetActive(false);

        if (nameEditDialog != null)
            nameEditDialog.CloseDialog();

        RefreshAccountInfo();
    }

    private void BindButtons()
    {
        if (settingbtn != null)
        {
            settingbtn.onClick.RemoveAllListeners();
            settingbtn.onClick.AddListener(OpenSettings);
        }

        if (mailboxbtn != null)
        {
            mailboxbtn.onClick.RemoveAllListeners();
            mailboxbtn.onClick.AddListener(OpenMailbox);
        }

        if (closeSettingsbtn != null)
        {
            closeSettingsbtn.onClick.RemoveAllListeners();
            closeSettingsbtn.onClick.AddListener(CloseSettings);
        }

        if (copyIdbtn != null)
        {
            copyIdbtn.onClick.RemoveAllListeners();
            copyIdbtn.onClick.AddListener(CopyAccountId);
        }

        if (editNamebtn != null)
        {
            editNamebtn.onClick.RemoveAllListeners();
            editNamebtn.onClick.AddListener(OpenEditNameDialog);
        }

        if (soundSettingsEntrybtn != null)
        {
            soundSettingsEntrybtn.onClick.RemoveAllListeners();
            soundSettingsEntrybtn.onClick.AddListener(OpenSoundSettings);
        }

        if (bindAccountEntrybtn != null)
        {
            bindAccountEntrybtn.onClick.RemoveAllListeners();
            bindAccountEntrybtn.onClick.AddListener(OpenBindAccount);
        }

        if (changePasswordEntrybtn != null)
        {
            changePasswordEntrybtn.onClick.RemoveAllListeners();
            changePasswordEntrybtn.onClick.AddListener(OpenChangePassword);
        }

        if (redeemCodeEntrybtn != null)
        {
            redeemCodeEntrybtn.onClick.RemoveAllListeners();
            redeemCodeEntrybtn.onClick.AddListener(OpenRedeemCode);
        }

        if (switchAccountbtn != null)
        {
            switchAccountbtn.onClick.RemoveAllListeners();
            switchAccountbtn.onClick.AddListener(SwitchAccount);
        }

        if (logoutbtn != null)
        {
            logoutbtn.onClick.RemoveAllListeners();
            logoutbtn.onClick.AddListener(Logout);
        }
    }

    public void OpenSettings()
    {
        RefreshAccountInfo();

        if (settingsOverlay != null)
        {
            settingsOverlay.SetActive(true);
            settingsOverlay.transform.SetAsLastSibling();
        }
    }

    public void CloseSettings()
    {
        if (settingsOverlay != null)
            settingsOverlay.SetActive(false);
    }

    private void OpenMailbox()
    {
        if (mailboxDialog != null)
            mailboxDialog.OpenDialog();
    }

    private void OpenEditNameDialog()
    {
        if (nameEditDialog == null)
            return;

        string currentName = PlayerPrefs.GetString(AccountNameKey, "Player");

        nameEditDialog.OpenDialog(currentName, SaveAccountName);
    }

    private void SaveAccountName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return;

        newName = newName.Trim();

        PlayerPrefs.SetString(AccountNameKey, newName);
        PlayerPrefs.Save();

        RefreshAccountInfo();
    }

    private void OpenSoundSettings()
    {
        if (soundSettingsDialog != null)
            soundSettingsDialog.OpenDialog();
    }

    private void OpenBindAccount()
    {
        if (bindAccountDialog != null)
            bindAccountDialog.OpenDialog();
    }

    private void OpenChangePassword()
    {
        if (changePasswordDialog != null)
            changePasswordDialog.OpenDialog();
    }

    private void OpenRedeemCode()
    {
        if (redeemCodeDialog != null)
            redeemCodeDialog.OpenDialog();
    }

    private void RefreshAccountInfo()
    {
        string name = PlayerPrefs.GetString(AccountNameKey, "Player");
        string id = PlayerPrefs.GetString(AccountIdKey, "BOOM-0000");

        if (accountNamelbl != null)
            accountNamelbl.text = name;

        if (accountIdlbl != null)
            accountIdlbl.text = "ID: " + id;

    }

    private void CopyAccountId()
    {
        string id = PlayerPrefs.GetString(AccountIdKey, "BOOM-0000");
        GUIUtility.systemCopyBuffer = id;
    }

    private void SwitchAccount()
    {
        ClearSession();
        SceneManager.LoadScene(authGateSceneName);
    }

    private void Logout()
    {
        ClearSession();
        SceneManager.LoadScene(authGateSceneName);
    }

    /// <summary>
    /// Xóa session local demo. Sau này thay bằng logout backend thật.
    /// </summary>
    private void ClearSession()
    {
        PlayerPrefs.DeleteKey(SessionKey);
        PlayerPrefs.DeleteKey(AccountEmailKey);
        PlayerPrefs.DeleteKey(AccountIdKey);
        PlayerPrefs.DeleteKey(AccountNameKey);
        PlayerPrefs.DeleteKey(AccountProviderKey);
        PlayerPrefs.Save();
    }

}
