using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Điều khiển màn hình đăng nhập, đăng ký và khôi phục phiên đăng nhập đã lưu.
/// Hiện tại dùng demo PlayerPrefs, sau này thay bằng backend/Nakama/Steam/Discord thật.
/// </summary>
public sealed class AuthGateUIController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Pages")]
    [SerializeField] private GameObject loginPage;
    [SerializeField] private GameObject registerPage;
    [SerializeField] private GameObject quickLoginPanel;
    [SerializeField] private GameObject loadingPanel;

    [Header("Tabs")]
    [SerializeField] private Button signInTabbtn;
    [SerializeField] private Button registerTabbtn;
    [SerializeField] private Image signInTabImage;
    [SerializeField] private Image registerTabImage;

    [Header("Login")]
    [SerializeField] private TMP_InputField loginEmailInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button loginbtn;
    [SerializeField] private Button forgotbtn;

    [Header("Register")]
    [SerializeField] private TMP_InputField registerEmailInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private Button createbtn;

    [Header("Quick Login")]
    [SerializeField] private Button steamLoginbtn;
    [SerializeField] private Button discordLoginbtn;
    [SerializeField] private Button guestLoginbtn;

    [Header("Retry")]
    [SerializeField] private Button retryConnectionbtn;

    [Header("Labels")]
    [SerializeField] private TMP_Text authFeedbacklbl;
    [SerializeField] private TMP_Text loadinglbl;

    [Header("Colors")]
    [SerializeField] private Color activeTabColor = new(0.43f, 0.84f, 0.56f, 1f);
    [SerializeField] private Color inactiveTabColor = new(0.18f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color normalFeedbackColor = new(0.66f, 0.7f, 0.68f, 1f);
    [SerializeField] private Color errorFeedbackColor = new(0.9f, 0.3f, 0.26f, 1f);

    private readonly List<Selectable> controls = new();

    private bool registerMode;
    private bool busy;

    private const string SessionKey = "AUTH_SESSION_ACTIVE";
    private const string AccountEmailKey = "AUTH_ACCOUNT_EMAIL";
    private const string AccountIdKey = "AUTH_ACCOUNT_ID";
    private const string AccountNameKey = "AUTH_ACCOUNT_NAME";
    private const string AccountProviderKey = "AUTH_ACCOUNT_PROVIDER";

    private async void Start()
    {
        CollectControls();
        SetupButtons();

        ShowMode(false);

        if (retryConnectionbtn != null)
            retryConnectionbtn.gameObject.SetActive(false);

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        await AttemptRestoreAsync();
    }

    private void CollectControls()
    {
        controls.Clear();

        AddControl(signInTabbtn);
        AddControl(registerTabbtn);

        AddControl(loginEmailInput);
        AddControl(loginPasswordInput);
        AddControl(loginbtn);
        AddControl(forgotbtn);

        AddControl(registerEmailInput);
        AddControl(registerPasswordInput);
        AddControl(confirmPasswordInput);
        AddControl(createbtn);

        AddControl(steamLoginbtn);
        AddControl(discordLoginbtn);
        AddControl(guestLoginbtn);

        AddControl(retryConnectionbtn);
    }

    private void AddControl(Selectable selectable)
    {
        if (selectable != null && !controls.Contains(selectable))
            controls.Add(selectable);
    }

    private void SetupButtons()
    {
        if (signInTabbtn != null)
        {
            signInTabbtn.onClick.RemoveAllListeners();
            signInTabbtn.onClick.AddListener(() => ShowMode(false));
        }

        if (registerTabbtn != null)
        {
            registerTabbtn.onClick.RemoveAllListeners();
            registerTabbtn.onClick.AddListener(() => ShowMode(true));
        }

        if (loginbtn != null)
        {
            loginbtn.onClick.RemoveAllListeners();
            loginbtn.onClick.AddListener(Login);
        }

        if (createbtn != null)
        {
            createbtn.onClick.RemoveAllListeners();
            createbtn.onClick.AddListener(Register);
        }

        if (forgotbtn != null)
        {
            forgotbtn.onClick.RemoveAllListeners();
            forgotbtn.onClick.AddListener(ForgotPassword);
        }

        if (steamLoginbtn != null)
        {
            steamLoginbtn.onClick.RemoveAllListeners();
            steamLoginbtn.onClick.AddListener(() => QuickLogin("Steam"));
        }

        if (discordLoginbtn != null)
        {
            discordLoginbtn.onClick.RemoveAllListeners();
            discordLoginbtn.onClick.AddListener(() => QuickLogin("Discord"));
        }

        if (guestLoginbtn != null)
        {
            guestLoginbtn.onClick.RemoveAllListeners();
            guestLoginbtn.onClick.AddListener(() => QuickLogin("Guest"));
        }

        if (retryConnectionbtn != null)
        {
            retryConnectionbtn.onClick.RemoveAllListeners();
            retryConnectionbtn.onClick.AddListener(RetryConnection);
        }

        if (loginPasswordInput != null)
        {
            loginPasswordInput.onSubmit.RemoveAllListeners();
            loginPasswordInput.onSubmit.AddListener(_ => Login());
        }

        if (confirmPasswordInput != null)
        {
            confirmPasswordInput.onSubmit.RemoveAllListeners();
            confirmPasswordInput.onSubmit.AddListener(_ => Register());
        }
    }

    /// <summary>
    /// Thử khôi phục phiên đăng nhập đã lưu trước khi yêu cầu người chơi login lại.
    /// </summary>
    private async Task AttemptRestoreAsync()
    {
        if (retryConnectionbtn != null)
            retryConnectionbtn.gameObject.SetActive(false);

        SetBusy(true, "Checking saved session...");

        try
        {
            bool restored = await TryRestoreSessionAsync();

            if (restored)
            {
                LoadMainMenu();
                return;
            }

            SetFeedback("Sign in to continue.");
        }
        catch (Exception exception)
        {
            SetFeedback(ToActionableError(exception), true);

            if (retryConnectionbtn != null)
                retryConnectionbtn.gameObject.SetActive(true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task<bool> TryRestoreSessionAsync()
    {
        await Task.Delay(450);

        bool hasSession = PlayerPrefs.GetInt(SessionKey, 0) == 1;

        if (!hasSession)
            return false;

        string accountId = PlayerPrefs.GetString(AccountIdKey, string.Empty);

        if (string.IsNullOrEmpty(accountId))
            return false;

        return true;
    }

    private async void RetryConnection()
    {
        if (busy)
            return;

        await AttemptRestoreAsync();
    }

    private void ShowMode(bool createAccount)
    {
        if (busy)
            return;

        registerMode = createAccount;

        if (loginPage != null)
            loginPage.SetActive(!registerMode);

        if (registerPage != null)
            registerPage.SetActive(registerMode);

        // Quick login chỉ hiện ở màn login để form đăng ký không bị chật.
        if (quickLoginPanel != null)
            quickLoginPanel.SetActive(!registerMode);

        if (signInTabImage != null)
            signInTabImage.color = registerMode ? inactiveTabColor : activeTabColor;

        if (registerTabImage != null)
            registerTabImage.color = registerMode ? activeTabColor : inactiveTabColor;

        SetFeedback(registerMode
            ? "Create account. Player ID will be generated automatically."
            : "Sign in to continue."
        );
    }

    private async void Login()
    {
        if (busy)
            return;

        string email = loginEmailInput != null
            ? loginEmailInput.text.Trim().ToLowerInvariant()
            : "";

        string password = loginPasswordInput != null
            ? loginPasswordInput.text
            : "";

        if (!IsValidEmail(email))
        {
            SetFeedback("Enter a valid email address.", true);
            return;
        }

        if (password.Length < 8)
        {
            SetFeedback("Password must contain at least 8 characters.", true);
            return;
        }

        SetBusy(true, "Signing in...");

        try
        {
            await SignInDemoAsync(email, password);
            LoadMainMenu();
        }
        catch (Exception exception)
        {
            SetFeedback(ToActionableError(exception), true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void Register()
    {
        if (busy)
            return;

        string email = registerEmailInput != null
            ? registerEmailInput.text.Trim().ToLowerInvariant()
            : "";

        string password = registerPasswordInput != null
            ? registerPasswordInput.text
            : "";

        string confirmPassword = confirmPasswordInput != null
            ? confirmPasswordInput.text
            : "";

        if (!IsValidEmail(email))
        {
            SetFeedback("Enter a valid email address.", true);
            return;
        }

        if (password.Length < 8)
        {
            SetFeedback("Password must contain at least 8 characters.", true);
            return;
        }

        if (password != confirmPassword)
        {
            SetFeedback("Passwords do not match.", true);
            return;
        }

        SetBusy(true, "Creating account...");

        try
        {
            await RegisterDemoAsync(email, password);
            LoadMainMenu();
        }
        catch (Exception exception)
        {
            SetFeedback(ToActionableError(exception), true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void QuickLogin(string provider)
    {
        if (busy)
            return;

        SetBusy(true, "Signing in with " + provider + "...");

        try
        {
            await QuickLoginDemoAsync(provider);
            LoadMainMenu();
        }
        catch (Exception exception)
        {
            SetFeedback(ToActionableError(exception), true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ForgotPassword()
    {
        SetFeedback("Password recovery will be added later.");
    }

    private async Task SignInDemoAsync(string email, string password)
    {
        await Task.Delay(600);

        string id = PlayerPrefs.GetString(AccountIdKey, string.Empty);

        if (string.IsNullOrEmpty(id))
            id = GenerateAccountId("BOOM");

        SaveSession(
            email,
            id,
            GetNameFromEmail(email),
            "Email"
        );
    }

    private async Task RegisterDemoAsync(string email, string password)
    {
        await Task.Delay(700);

        SaveSession(
            email,
            GenerateAccountId("BOOM"),
            GetNameFromEmail(email),
            "Email"
        );
    }

    private async Task QuickLoginDemoAsync(string provider)
    {
        await Task.Delay(500);

        string prefix = provider.ToUpperInvariant();

        SaveSession(
            provider.ToLowerInvariant() + "@quick.login",
            GenerateAccountId(prefix),
            provider + "User",
            provider
        );
    }

    /// <summary>
    /// Lưu phiên demo. Sau này thay bằng token/session từ backend.
    /// </summary>
    private void SaveSession(string email, string accountId, string accountName, string provider)
    {
        PlayerPrefs.SetInt(SessionKey, 1);
        PlayerPrefs.SetString(AccountEmailKey, email);
        PlayerPrefs.SetString(AccountIdKey, accountId);
        PlayerPrefs.SetString(AccountNameKey, accountName);
        PlayerPrefs.SetString(AccountProviderKey, provider);
        PlayerPrefs.Save();
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SetBusy(bool value, string message = null)
    {
        busy = value;

        for (int i = 0; i < controls.Count; i++)
        {
            if (controls[i] != null)
                controls[i].interactable = !value;
        }

        if (loadingPanel != null)
            loadingPanel.SetActive(value);

        if (loadinglbl != null && !string.IsNullOrWhiteSpace(message))
            loadinglbl.text = message;

        if (!string.IsNullOrWhiteSpace(message))
            SetFeedback(message);
    }

    private void SetFeedback(string message, bool error = false)
    {
        if (authFeedbacklbl == null)
            return;

        authFeedbacklbl.text = message;
        authFeedbacklbl.color = error ? errorFeedbackColor : normalFeedbackColor;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(email) && new MailAddress(email).Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static string ToActionableError(Exception exception)
    {
        string message = exception?.Message ?? string.Empty;
        string lower = message.ToLowerInvariant();

        if (lower.Contains("refused") ||
            lower.Contains("connectfailure") ||
            lower.Contains("sending the request"))
            return "Bommy services are unavailable. Check the server and try again.";

        if (lower.Contains("unauthenticated") ||
            lower.Contains("credentials") ||
            lower.Contains("401"))
            return "Email or password is incorrect.";

        if (lower.Contains("already exists") ||
            lower.Contains("in use") ||
            lower.Contains("409"))
            return "That email is already in use.";

        message = message.Replace('\r', ' ').Replace('\n', ' ').Trim();

        return string.IsNullOrWhiteSpace(message)
            ? "The account request failed. Try again."
            : message.Length <= 180 ? message : message[..180] + "...";
    }

    private static string GetNameFromEmail(string email)
    {
        int index = email.IndexOf('@');

        if (index <= 0)
            return "Player";

        string name = email[..index];

        if (string.IsNullOrWhiteSpace(name))
            return "Player";

        return name;
    }

    private static string GenerateAccountId(string prefix)
    {
        int number = UnityEngine.Random.Range(1000, 9999);
        return prefix + "-" + number;
    }
}
