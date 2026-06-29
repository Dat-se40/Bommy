using System.Threading.Tasks;
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
    [SerializeField] private string authGateSceneName = "AuthGate";

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

    private AuthService authService;

    private void Awake()
    {
        // Just bind buttons here — no AuthService needed yet
        BindButtons();

        if (settingsOverlay != null)
            settingsOverlay.SetActive(false);

        if (nameEditDialog != null)
            nameEditDialog.CloseDialog();
    }

    private void Start()
    {
        // Bootstrapper.Awake() has already run by now, AuthService exists
        authService = FindAnyObjectByType<AuthService>();

        if (authService == null)
        {
            Debug.LogError("[MainMenuAccountUIController] AuthService not found.");
            return;
        }

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

        string currentName = authService?.Session?.Username ?? "Player";
        nameEditDialog.OpenDialog(currentName, SaveAccountName);
    }

    private async void SaveAccountName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || authService == null)
            return;

        newName = newName.Trim();

        AuthResult result = await authService.UpdateDisplayNameAsync(newName);

        if (result.Success)
            RefreshAccountInfo();
        else
            Debug.LogWarning("[MainMenuAccountUIController] Failed to update name: " + result.Error);
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
        // Pull directly from the live session — no PlayerPrefs needed
        string name = authService.DisplayName;
        string username = authService.Username;

        if (accountNamelbl != null)
            accountNamelbl.text = name;

        if (accountIdlbl != null)
            accountIdlbl.text = "Username: " + (string.IsNullOrWhiteSpace(username) ? "—" : username);
    }

    private void CopyAccountId()
    {
        string username = authService.Username;
        if (!string.IsNullOrWhiteSpace(username))
        {
            GUIUtility.systemCopyBuffer = username;
        }
    }

    private async void SwitchAccount()
    {
        await SignOutAndReturn();
    }

    private async void Logout()
    {
        await SignOutAndReturn();
    }

    private async Task SignOutAndReturn()
    {
        if (authService != null)
            await authService.LogoutAsync();

        SceneManager.LoadScene(authGateSceneName);
    }
}
