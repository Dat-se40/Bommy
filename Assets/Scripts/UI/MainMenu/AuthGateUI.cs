using System;
using System.Net.Mail;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Authentication gate for email sign-in, registration, and saved-session restore.
/// </summary>
public sealed class AuthGateUI : MonoBehaviour
{
    const string SceneName = "AuthGate";
    static readonly Regex HandlePattern = new("^[a-z0-9_]{3,20}$", RegexOptions.Compiled);
    static readonly Color OverlayColor = new(0.025f, 0.03f, 0.035f, 0.88f);
    static readonly Color PanelColor = new(0.11f, 0.13f, 0.14f, 0.98f);
    static readonly Color FieldColor = new(0.055f, 0.07f, 0.075f, 1f);
    static readonly Color TextColor = new(0.96f, 0.94f, 0.84f, 1f);
    static readonly Color MutedColor = new(0.66f, 0.7f, 0.68f, 1f);
    static readonly Color AccentColor = new(0.43f, 0.84f, 0.56f, 1f);
    static readonly Color WarmColor = new(0.96f, 0.73f, 0.28f, 1f);
    static readonly Color DangerColor = new(0.9f, 0.3f, 0.26f, 1f);

    readonly System.Collections.Generic.List<Selectable> controls = new();
    TMP_InputField emailInput;
    TMP_InputField handleInput;
    TMP_InputField passwordInput;
    TMP_InputField confirmPasswordInput;
    TMP_Text titleLabel;
    TMP_Text subtitleLabel;
    TMP_Text feedbackLabel;
    Button submitButton;
    TMP_Text submitLabel;
    Button signInTab;
    Button registerTab;
    Button retryButton;
    bool registerMode;
    bool busy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InstallForActiveScene()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryInstall(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryInstall(scene);
    }

    static void TryInstall(Scene scene)
    {
        if (scene.name != SceneName || FindAnyObjectByType<AuthGateUI>() != null)
            return;

        GameObject canvasObject = new("AuthGateCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject host = new("AuthGate", typeof(RectTransform));
        host.transform.SetParent(canvasObject.transform, false);
        Stretch(host.GetComponent<RectTransform>());
        host.AddComponent<AuthGateUI>();
    }

    async void Start()
    {
        Build();
        ShowMode(false);
        await AttemptRestoreAsync();
    }

    async System.Threading.Tasks.Task AttemptRestoreAsync()
    {
        retryButton.gameObject.SetActive(false);
        SetBusy(true, "Checking saved session...");

        try
        {
            bool restored = await AccountSessionCoordinator.EnsureExists().TryRestoreSessionAsync();

            if (!restored)
                SetFeedback("Sign in to continue.");
        }
        catch (Exception exception)
        {
            SetFeedback(ToActionableError(exception), true);
            retryButton.gameObject.SetActive(true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    void Build()
    {
        GameObject overlay = CreateRect(transform, "Backdrop", OverlayColor);
        Stretch(overlay.GetComponent<RectTransform>());

        TMP_Text brand = CreateText(overlay.transform, "Brand", "BOMMY", 56f, TextColor, FontStyles.Bold);
        brand.alignment = TextAlignmentOptions.Center;
        SetAnchored(brand.rectTransform, 0f, 1f, 1f, 1f, 24f, -112f, -24f, -36f);

        TMP_Text kicker = CreateText(overlay.transform, "Kicker", "ONLINE ACCOUNT", 14f, WarmColor, FontStyles.Bold);
        kicker.alignment = TextAlignmentOptions.Center;
        SetAnchored(kicker.rectTransform, 0f, 1f, 1f, 1f, 24f, -142f, -24f, -112f);

        GameObject panel = CreateRect(overlay.transform, "CredentialPanel", PanelColor);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(520f, 650f);
        panelRect.anchoredPosition = new Vector2(0f, -45f);

        titleLabel = CreateText(panel.transform, "Title", "SIGN IN", 28f, TextColor, FontStyles.Bold);
        titleLabel.alignment = TextAlignmentOptions.Center;
        SetAnchored(titleLabel.rectTransform, 0f, 1f, 1f, 1f, 28f, -68f, -28f, -24f);

        subtitleLabel = CreateText(panel.transform, "Subtitle", "Use your Bommy account to continue.", 14f, MutedColor);
        subtitleLabel.alignment = TextAlignmentOptions.Center;
        SetAnchored(subtitleLabel.rectTransform, 0f, 1f, 1f, 1f, 28f, -100f, -28f, -68f);

        GameObject tabs = CreateRect(panel.transform, "ModeTabs", FieldColor);
        SetAnchored(tabs.GetComponent<RectTransform>(), 0f, 1f, 1f, 1f, 28f, -162f, -28f, -114f);
        signInTab = CreateButton(tabs.transform, "SignInTab", "SIGN IN", AccentColor, () => ShowMode(false));
        SetAnchored(signInTab.GetComponent<RectTransform>(), 0f, 0f, 0.5f, 1f, 0f, 0f, -4f, 0f);
        registerTab = CreateButton(tabs.transform, "RegisterTab", "CREATE ACCOUNT", FieldColor, () => ShowMode(true));
        SetAnchored(registerTab.GetComponent<RectTransform>(), 0.5f, 0f, 1f, 1f, 4f, 0f, 0f, 0f);
        controls.Add(signInTab);
        controls.Add(registerTab);

        emailInput = CreateInput(panel.transform, "EmailInput", "Email address", TMP_InputField.ContentType.EmailAddress);
        SetAnchored(emailInput.GetComponent<RectTransform>(), 0f, 1f, 1f, 1f, 28f, -226f, -28f, -178f);

        handleInput = CreateInput(panel.transform, "HandleInput", "Public handle", TMP_InputField.ContentType.Standard);
        handleInput.characterLimit = 20;
        SetAnchored(handleInput.GetComponent<RectTransform>(), 0f, 1f, 1f, 1f, 28f, -290f, -28f, -242f);

        passwordInput = CreateInput(panel.transform, "PasswordInput", "Password", TMP_InputField.ContentType.Password);
        SetAnchored(passwordInput.GetComponent<RectTransform>(), 0f, 1f, 1f, 1f, 28f, -354f, -28f, -306f);

        confirmPasswordInput = CreateInput(panel.transform, "ConfirmPasswordInput", "Confirm password", TMP_InputField.ContentType.Password);
        SetAnchored(confirmPasswordInput.GetComponent<RectTransform>(), 0f, 1f, 1f, 1f, 28f, -418f, -28f, -370f);

        controls.Add(emailInput);
        controls.Add(handleInput);
        controls.Add(passwordInput);
        controls.Add(confirmPasswordInput);

        submitButton = CreateButton(panel.transform, "Submit", "CONTINUE", AccentColor, Submit);
        submitLabel = submitButton.GetComponentInChildren<TMP_Text>();
        SetAnchored(submitButton.GetComponent<RectTransform>(), 0f, 0f, 1f, 0f, 28f, 116f, -28f, 168f);
        controls.Add(submitButton);

        feedbackLabel = CreateText(panel.transform, "Feedback", string.Empty, 13f, MutedColor);
        feedbackLabel.alignment = TextAlignmentOptions.Center;
        feedbackLabel.textWrappingMode = TextWrappingModes.Normal;
        SetAnchored(feedbackLabel.rectTransform, 0f, 0f, 1f, 0f, 28f, 36f, -28f, 104f);

        retryButton = CreateButton(panel.transform, "Retry", "RETRY CONNECTION", WarmColor, RetryConnection);
        SetAnchored(retryButton.GetComponent<RectTransform>(), 0.25f, 0f, 0.75f, 0f, 0f, 8f, 0f, 38f);
        retryButton.gameObject.SetActive(false);
    }

    void ShowMode(bool createAccount)
    {
        if (busy)
            return;

        registerMode = createAccount;
        titleLabel.text = registerMode ? "CREATE ACCOUNT" : "SIGN IN";
        subtitleLabel.text = registerMode
            ? "Your public handle cannot be changed later."
            : "Use your Bommy account to continue.";
        handleInput.gameObject.SetActive(registerMode);
        confirmPasswordInput.gameObject.SetActive(registerMode);
        submitLabel.text = registerMode ? "CREATE ACCOUNT" : "SIGN IN";
        signInTab.GetComponent<Image>().color = registerMode ? FieldColor : AccentColor;
        registerTab.GetComponent<Image>().color = registerMode ? AccentColor : FieldColor;
        SetFeedback(string.Empty);
    }

    async void Submit()
    {
        if (busy)
            return;

        string email = emailInput.text.Trim().ToLowerInvariant();
        string password = passwordInput.text;
        string handle = handleInput.text.Trim().ToLowerInvariant();

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

        if (registerMode && !HandlePattern.IsMatch(handle))
        {
            SetFeedback("Handle must be 3-20 lowercase letters, numbers, or underscores.", true);
            return;
        }

        if (registerMode && password != confirmPasswordInput.text)
        {
            SetFeedback("Passwords do not match.", true);
            return;
        }

        SetBusy(true, registerMode ? "Creating account..." : "Signing in...");
        retryButton.gameObject.SetActive(false);

        try
        {
            AccountSessionCoordinator coordinator = AccountSessionCoordinator.EnsureExists();

            if (registerMode)
                await coordinator.RegisterAsync(email, handle, password);
            else
                await coordinator.SignInAsync(email, password);
        }
        catch (Exception exception)
        {
            SetFeedback(ToActionableError(exception), true);
            SetBusy(false);
        }
    }

    async void RetryConnection()
    {
        if (!busy)
            await AttemptRestoreAsync();
    }

    void SetBusy(bool value, string message = null)
    {
        busy = value;

        for (int i = 0; i < controls.Count; i++)
        {
            if (controls[i] != null)
                controls[i].interactable = !value;
        }

        if (!string.IsNullOrWhiteSpace(message))
            SetFeedback(message);
    }

    void SetFeedback(string message, bool error = false)
    {
        if (feedbackLabel == null)
            return;

        feedbackLabel.text = message;
        feedbackLabel.color = error ? DangerColor : MutedColor;
    }

    static bool IsValidEmail(string email)
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

    static string ToActionableError(Exception exception)
    {
        string message = exception?.Message ?? string.Empty;
        string lower = message.ToLowerInvariant();

        if (lower.Contains("refused") || lower.Contains("connectfailure") || lower.Contains("sending the request"))
            return "Bommy services are unavailable. Check the server and try again.";

        if (lower.Contains("unauthenticated") || lower.Contains("credentials") || lower.Contains("401"))
            return "Email or password is incorrect.";

        if (lower.Contains("already exists") || lower.Contains("in use") || lower.Contains("409"))
            return "That email or handle is already in use.";

        message = message.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return string.IsNullOrWhiteSpace(message)
            ? "The account request failed. Try again."
            : message.Length <= 180 ? message : message[..180] + "...";
    }

    static GameObject CreateRect(Transform parent, string name, Color color)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = color.a > 0f;
        return gameObject;
    }

    static TMP_Text CreateText(Transform parent, string name, string value, float size, Color color, FontStyles style = FontStyles.Normal)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    static Button CreateButton(Transform parent, string name, string label, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject root = CreateRect(parent, name, color);
        Button button = root.AddComponent<Button>();
        button.onClick.AddListener(action);
        TMP_Text text = CreateText(root.transform, "Label", label, 14f, PanelColor, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        Stretch(text.rectTransform, 6f);
        return button;
    }

    static TMP_InputField CreateInput(Transform parent, string name, string placeholderValue, TMP_InputField.ContentType contentType)
    {
        GameObject root = CreateRect(parent, name, FieldColor);
        TMP_InputField input = root.AddComponent<TMP_InputField>();
        GameObject viewport = CreateRect(root.transform, "Text Area", Color.clear);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect, 14f, 8f);
        viewport.AddComponent<RectMask2D>();
        TMP_Text placeholder = CreateText(viewport.transform, "Placeholder", placeholderValue, 15f, MutedColor);
        placeholder.fontStyle = FontStyles.Italic;
        Stretch(placeholder.rectTransform);
        TMP_Text value = CreateText(viewport.transform, "Text", string.Empty, 15f, TextColor);
        Stretch(value.rectTransform);
        input.textViewport = viewportRect;
        input.textComponent = value;
        input.placeholder = placeholder;
        input.contentType = contentType;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.caretColor = AccentColor;
        input.selectionColor = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.35f);
        return input;
    }

    static void Stretch(RectTransform rect, float inset = 0f)
    {
        Stretch(rect, inset, inset);
    }

    static void Stretch(RectTransform rect, float horizontalInset, float verticalInset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontalInset, verticalInset);
        rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
    }

    static void SetAnchored(RectTransform rect, float minX, float minY, float maxX, float maxY, float left, float bottom, float right, float top)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }
}
