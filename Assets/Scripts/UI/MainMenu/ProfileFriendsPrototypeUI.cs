using System;
using System.Collections.Generic;
using Nakama;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runtime-built profile, account, and friends screen backed by Nakama.
/// </summary>
public sealed class ProfileFriendsPrototypeUI : MonoBehaviour
{
    const string MainMenuSceneName = "MainMenu";
    static readonly Color OverlayColor = new(0.03f, 0.04f, 0.05f, 0.78f);
    static readonly Color PanelColor = new(0.12f, 0.14f, 0.15f, 0.98f);
    static readonly Color RaisedColor = new(0.18f, 0.2f, 0.2f, 1f);
    static readonly Color FieldColor = new(0.08f, 0.1f, 0.1f, 1f);
    static readonly Color TextColor = new(0.96f, 0.94f, 0.84f, 1f);
    static readonly Color MutedTextColor = new(0.66f, 0.7f, 0.68f, 1f);
    static readonly Color AccentColor = new(0.43f, 0.84f, 0.56f, 1f);
    static readonly Color WarmColor = new(0.96f, 0.73f, 0.28f, 1f);
    static readonly Color DangerColor = new(0.88f, 0.31f, 0.28f, 1f);

    readonly List<FriendModel> friends = new();
    readonly List<FriendModel> requests = new();
    readonly List<Button> tabButtons = new();
    readonly List<GameObject> tabPages = new();
    readonly List<Selectable> backendControls = new();

    NakamaConnectionManager manager;
    RectTransform windowRect;
    GameObject overlay;
    TMP_Text profileSummaryLabel;
    TMP_Text accountSummaryLabel;
    TMP_Text feedbackLabel;
    TMP_InputField displayNameInput;
    TMP_InputField friendSearchInput;
    TMP_InputField friendIdInput;
    TMP_Text profilePreviewName;
    Transform friendList;
    Transform requestList;
    string displayName;
    string username;
    bool busy;
    int activeTab;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterSceneInstaller()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (scene.name != MainMenuSceneName)
            return;

        EnsureExists(scene);
    }

    public static ProfileFriendsPrototypeUI EnsureExists()
    {
        return EnsureExists(SceneManager.GetActiveScene());
    }

    static ProfileFriendsPrototypeUI EnsureExists(Scene scene)
    {
        ProfileFriendsPrototypeUI existing = FindAnyObjectByType<ProfileFriendsPrototypeUI>();

        if (existing != null)
            return existing;

        Canvas canvas = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            canvas = root.GetComponentInChildren<Canvas>(true);

            if (canvas != null)
                break;
        }

        if (canvas == null)
            return null;

        GameObject host = new("ProfileFriendsPrototypeUI", typeof(RectTransform));
        host.transform.SetParent(canvas.transform, false);
        host.layer = canvas.gameObject.layer;
        Stretch(host.GetComponent<RectTransform>());
        host.transform.SetAsLastSibling();
        return host.AddComponent<ProfileFriendsPrototypeUI>();
    }

    public static void ResetRuntimeState()
    {
        ProfileFriendsPrototypeUI current = FindAnyObjectByType<ProfileFriendsPrototypeUI>();

        if (current == null)
            return;

        current.friends.Clear();
        current.requests.Clear();
        current.displayName = "Player";
        current.username = string.Empty;
        current.RefreshFriends();
        current.RefreshRequests();

        if (current.overlay != null)
            current.overlay.SetActive(false);
    }

    void Awake()
    {
        manager = NakamaConnectionManager.EnsureExists();
        displayName = manager.DisplayName;
        username = manager.Username;
        BuildScreen();
        ShowTab(0);
        overlay.SetActive(false);
    }

    void OnEnable()
    {
        NakamaConnectionManager.AccountChanged += OnAccountChanged;
    }

    void OnDisable()
    {
        NakamaConnectionManager.AccountChanged -= OnAccountChanged;
    }

    void Update()
    {
        if (overlay != null && overlay.activeSelf && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            Close();
    }

    void OnRectTransformDimensionsChange()
    {
        FitWindowToCanvas();
    }

    void BuildScreen()
    {
        overlay = CreateRect(transform, "ProfileFriendsOverlay", OverlayColor);
        Stretch(overlay.GetComponent<RectTransform>());

        Button dismissArea = overlay.AddComponent<Button>();
        dismissArea.transition = Selectable.Transition.None;
        dismissArea.onClick.AddListener(Close);

        GameObject window = CreateRect(overlay.transform, "Window", PanelColor);
        windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        window.AddComponent<Button>().transition = Selectable.Transition.None;
        FitWindowToCanvas();

        BuildHeader(window.transform);
        BuildTabs(window.transform);
        BuildPages(window.transform);
        BuildFooter(window.transform);
    }

    void BuildHeader(Transform parent)
    {
        GameObject header = CreateRect(parent, "Header", RaisedColor);
        SetAnchored(header.GetComponent<RectTransform>(), 0f, 1f, 1f, 1f, 0f, -72f, 0f, 0f);

        TMP_Text eyebrow = CreateText(header.transform, "Eyebrow", "PLAYER HUB", 13f, WarmColor, FontStyles.Bold);
        SetAnchored(eyebrow.rectTransform, 0f, 0f, 0.5f, 1f, 24f, 42f, -8f, -10f);

        TMP_Text title = CreateText(header.transform, "Title", "PROFILE & FRIENDS", 27f, TextColor, FontStyles.Bold);
        SetAnchored(title.rectTransform, 0f, 0f, 0.75f, 1f, 24f, 8f, -8f, -28f);

        profileSummaryLabel = CreateText(header.transform, "ProfileSummary", string.Empty, 14f, MutedTextColor);
        profileSummaryLabel.alignment = TextAlignmentOptions.MidlineRight;
        SetAnchored(profileSummaryLabel.rectTransform, 0.5f, 0f, 1f, 1f, 0f, 8f, -76f, -8f);

        Button close = CreateButton(header.transform, "Close", "X", DangerColor, Close);
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 0.5f);
        closeRect.pivot = new Vector2(1f, 0.5f);
        closeRect.anchoredPosition = new Vector2(-18f, 0f);
        closeRect.sizeDelta = new Vector2(42f, 42f);

        RefreshProfileSummary();
    }

    void BuildTabs(Transform parent)
    {
        GameObject tabs = CreateRect(parent, "Tabs", FieldColor);
        SetAnchored(tabs.GetComponent<RectTransform>(), 0f, 1f, 1f, 1f, 0f, -124f, 0f, -72f);

        HorizontalLayoutGroup layout = tabs.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 8, 8);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        AddTabButton(tabs.transform, "PROFILE", 0);
        AddTabButton(tabs.transform, "ACCOUNT", 1);
        AddTabButton(tabs.transform, "FRIENDS", 2);
    }

    void AddTabButton(Transform parent, string label, int index)
    {
        Button button = CreateButton(parent, label + "Tab", label, RaisedColor, () => ShowTab(index));
        tabButtons.Add(button);
    }

    void BuildPages(Transform parent)
    {
        GameObject viewport = CreateRect(parent, "PageViewport", PanelColor);
        SetAnchored(viewport.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f, 0f, 38f, 0f, -124f);

        GameObject profilePage = CreatePage(viewport.transform, "ProfilePage");
        BuildProfilePage(profilePage.transform);
        tabPages.Add(profilePage);

        GameObject accountPage = CreatePage(viewport.transform, "AccountPage");
        BuildAccountPage(accountPage.transform);
        tabPages.Add(accountPage);

        GameObject friendsPage = CreatePage(viewport.transform, "FriendsPage");
        BuildFriendsPage(friendsPage.transform);
        tabPages.Add(friendsPage);
    }

    GameObject CreatePage(Transform parent, string name)
    {
        GameObject page = CreateRect(parent, name, Color.clear);
        Stretch(page.GetComponent<RectTransform>());
        return page;
    }

    void BuildProfilePage(Transform parent)
    {
        TMP_Text heading = CreateText(parent, "Heading", "MAKE IT YOURS", 22f, TextColor, FontStyles.Bold);
        SetAnchored(heading.rectTransform, 0f, 1f, 0.6f, 1f, 28f, -58f, 0f, -20f);

        TMP_Text caption = CreateText(parent, "Caption", "This is how other players will see you.", 14f, MutedTextColor);
        SetAnchored(caption.rectTransform, 0f, 1f, 0.8f, 1f, 28f, -88f, 0f, -58f);

        CreateFieldLabel(parent, "DISPLAY NAME", 28f, -112f, 0.54f);
        displayNameInput = CreateInput(parent, "DisplayNameInput", displayName, "Enter a display name");
        SetAnchored(displayNameInput.GetComponent<RectTransform>(), 0f, 1f, 0.54f, 1f, 28f, -182f, -12f, -138f);
        displayNameInput.characterLimit = 20;

        GameObject preview = CreateRect(parent, "IdentityPreview", RaisedColor);
        SetAnchored(preview.GetComponent<RectTransform>(), 0.58f, 0f, 1f, 1f, 0f, 28f, -28f, -28f);

        TMP_Text previewTag = CreateText(preview.transform, "Tag", "PLAYER CARD", 13f, WarmColor, FontStyles.Bold);
        SetAnchored(previewTag.rectTransform, 0f, 1f, 1f, 1f, 22f, -48f, -22f, -18f);

        profilePreviewName = CreateText(preview.transform, "Name", displayName.ToUpperInvariant(), 30f, TextColor, FontStyles.Bold);
        profilePreviewName.alignment = TextAlignmentOptions.Center;
        SetAnchored(profilePreviewName.rectTransform, 0f, 0.45f, 1f, 0.75f, 22f, 0f, -22f, 0f);

        TMP_Text character = CreateText(preview.transform, "Character", "SELECTED CHARACTER  /  MIMI", 14f, MutedTextColor);
        character.alignment = TextAlignmentOptions.Center;
        SetAnchored(character.rectTransform, 0f, 0.25f, 1f, 0.45f, 18f, 0f, -18f, 0f);

        Button save = TrackBackendControl(CreateButton(parent, "SaveProfile", "SAVE PROFILE", AccentColor, SaveProfile));
        SetAnchored(save.GetComponent<RectTransform>(), 0f, 0f, 0.54f, 0f, 28f, 28f, -12f, 76f);
        TrackBackendControl(displayNameInput);
    }

    void BuildAccountPage(Transform parent)
    {
        TMP_Text heading = CreateText(parent, "Heading", "KEEP YOUR PROGRESS", 22f, TextColor, FontStyles.Bold);
        SetAnchored(heading.rectTransform, 0f, 1f, 0.7f, 1f, 28f, -58f, 0f, -20f);

        accountSummaryLabel = CreateText(parent, "AccountSummary", string.Empty, 14f, AccentColor, FontStyles.Bold);
        SetAnchored(accountSummaryLabel.rectTransform, 0f, 1f, 1f, 1f, 28f, -92f, -28f, -60f);

        CreateFieldLabel(parent, "PUBLIC HANDLE", 28f, -120f, 0.55f);
        TMP_Text handleValue = CreateText(parent, "HandleValue", "@" + username, 18f, TextColor, FontStyles.Bold);
        SetAnchored(handleValue.rectTransform, 0f, 1f, 1f, 1f, 28f, -184f, -28f, -142f);

        GameObject divider = CreateRect(parent, "Divider", MutedTextColor);
        SetAnchored(divider.GetComponent<RectTransform>(), 0f, 1f, 1f, 1f, 28f, -220f, -28f, -218f);

        CreateFieldLabel(parent, "EMAIL", 28f, -252f, 0.55f);
        TMP_Text emailValue = CreateText(parent, "EmailValue", MaskEmail(manager?.Account?.Email ?? string.Empty), 18f, TextColor, FontStyles.Bold);
        SetAnchored(emailValue.rectTransform, 0f, 1f, 1f, 1f, 28f, -316f, -28f, -274f);

        Button switchAccount = TrackBackendControl(CreateButton(parent, "SwitchAccount", "SWITCH ACCOUNT", WarmColor, SwitchAccount));
        SetAnchored(switchAccount.GetComponent<RectTransform>(), 0f, 0f, 0.48f, 0f, 28f, 28f, -8f, 76f);

        Button logout = TrackBackendControl(CreateButton(parent, "Logout", "LOG OUT", DangerColor, Logout));
        SetAnchored(logout.GetComponent<RectTransform>(), 0.52f, 0f, 1f, 0f, 8f, 28f, -28f, 76f);

        TMP_Text note = CreateText(parent, "Note", "Your handle is permanent. Your display name can be changed in Profile.", 13f, MutedTextColor);
        SetAnchored(note.rectTransform, 0f, 0f, 1f, 0f, 28f, 88f, -28f, 122f);
        RefreshAccountSummary();
    }

    void BuildFriendsPage(Transform parent)
    {
        GameObject controls = CreateRect(parent, "FriendControls", Color.clear);
        SetAnchored(controls.GetComponent<RectTransform>(), 0f, 1f, 1f, 1f, 28f, -82f, -28f, -24f);

        friendSearchInput = CreateInput(controls.transform, "SearchInput", string.Empty, "Search friends");
        SetAnchored(friendSearchInput.GetComponent<RectTransform>(), 0f, 0f, 0.48f, 1f, 0f, 0f, -8f, 0f);
        friendSearchInput.onValueChanged.AddListener(_ => RefreshFriends());

        friendIdInput = CreateInput(controls.transform, "FriendIdInput", string.Empty, "Player ID or username");
        SetAnchored(friendIdInput.GetComponent<RectTransform>(), 0.5f, 0f, 0.82f, 1f, 0f, 0f, -8f, 0f);

        Button add = TrackBackendControl(CreateButton(controls.transform, "AddFriend", "ADD", AccentColor, AddFriend));
        SetAnchored(add.GetComponent<RectTransform>(), 0.84f, 0f, 1f, 1f, 0f, 0f, 0f, 0f);
        TrackBackendControl(friendIdInput);

        TMP_Text friendsHeading = CreateText(parent, "FriendsHeading", "FRIENDS", 15f, WarmColor, FontStyles.Bold);
        SetAnchored(friendsHeading.rectTransform, 0f, 1f, 0.6f, 1f, 28f, -116f, 0f, -88f);

        TMP_Text requestsHeading = CreateText(parent, "RequestsHeading", "REQUESTS", 15f, WarmColor, FontStyles.Bold);
        SetAnchored(requestsHeading.rectTransform, 0.62f, 1f, 1f, 1f, 0f, -116f, -28f, -88f);

        GameObject friendsPanel = CreateRect(parent, "FriendList", FieldColor);
        SetAnchored(friendsPanel.GetComponent<RectTransform>(), 0f, 0f, 0.6f, 1f, 28f, 24f, -10f, -120f);
        friendList = friendsPanel.transform;

        GameObject requestsPanel = CreateRect(parent, "RequestList", FieldColor);
        SetAnchored(requestsPanel.GetComponent<RectTransform>(), 0.62f, 0f, 1f, 1f, 0f, 24f, -28f, -120f);
        requestList = requestsPanel.transform;

        RefreshFriends();
        RefreshRequests();
    }

    void BuildFooter(Transform parent)
    {
        GameObject footer = CreateRect(parent, "Footer", FieldColor);
        SetAnchored(footer.GetComponent<RectTransform>(), 0f, 0f, 1f, 0f, 0f, 0f, 0f, 38f);

        feedbackLabel = CreateText(footer.transform, "Feedback", "Connect to Nakama to manage your profile.", 13f, MutedTextColor);
        SetAnchored(feedbackLabel.rectTransform, 0f, 0f, 1f, 1f, 18f, 0f, -18f, 0f);
    }

    public async void Open()
    {
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        await RefreshBackendStateAsync();
    }

    public void Close()
    {
        overlay.SetActive(false);
    }

    void ShowTab(int index)
    {
        activeTab = Mathf.Clamp(index, 0, tabPages.Count - 1);

        for (int i = 0; i < tabPages.Count; i++)
        {
            tabPages[i].SetActive(i == activeTab);
            tabButtons[i].GetComponent<Image>().color = i == activeTab ? AccentColor : RaisedColor;
        }
    }

    async void SaveProfile()
    {
        string nextName = displayNameInput.text.Trim();

        if (nextName.Length < 2 || nextName.Length > 20)
        {
            SetFeedback("Display name must contain 2-20 characters.", true);
            return;
        }

        SetBusy(true);

        try
        {
            await manager.UpdateDisplayNameAsync(nextName);
            ApplyAccount();
            SetFeedback("Display name saved as " + displayName + ".");
        }
        catch (Exception exception)
        {
            SetFeedback(GetErrorMessage(exception), true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    async void SwitchAccount()
    {
        SetBusy(true);
        await AccountSessionCoordinator.EnsureExists().SwitchAccountAsync();
    }

    async void Logout()
    {
        SetBusy(true);
        await AccountSessionCoordinator.EnsureExists().LogoutAsync();
    }

    async void AddFriend()
    {
        string identifier = friendIdInput.text.Trim();

        if (string.IsNullOrEmpty(identifier))
        {
            SetFeedback("Enter a player ID or username.", true);
            return;
        }

        SetBusy(true);

        try
        {
            await manager.AddFriendAsync(identifier);
            friendIdInput.text = string.Empty;
            await LoadFriendsAsync();
            SetFeedback("Friend request sent to " + identifier + ".");
        }
        catch (Exception exception)
        {
            SetFeedback(GetErrorMessage(exception), true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    async void AcceptRequest(FriendModel request)
    {
        SetBusy(true);

        try
        {
            await manager.AcceptFriendAsync(request.Id);
            await LoadFriendsAsync();
            SetFeedback("Accepted " + request.DisplayName + ".");
        }
        catch (Exception exception)
        {
            SetFeedback(GetErrorMessage(exception), true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    async void DeleteRequest(FriendModel request)
    {
        SetBusy(true);

        try
        {
            await manager.DeleteFriendAsync(request.Id);
            await LoadFriendsAsync();
            SetFeedback((request.IsIncoming ? "Declined " : "Cancelled request to ") + request.DisplayName + ".");
        }
        catch (Exception exception)
        {
            SetFeedback(GetErrorMessage(exception), true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    async void RemoveFriend(FriendModel friend)
    {
        SetBusy(true);

        try
        {
            await manager.DeleteFriendAsync(friend.Id);
            await LoadFriendsAsync();
            SetFeedback("Removed " + friend.DisplayName + ".");
        }
        catch (Exception exception)
        {
            SetFeedback(GetErrorMessage(exception), true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    async System.Threading.Tasks.Task RefreshBackendStateAsync()
    {
        SetBusy(true);
        SetFeedback("Loading Nakama account...");

        try
        {
            if (!manager.IsAuthenticated)
                throw new InvalidOperationException("Your session is no longer authenticated. Log in again.");

            if (manager.Account == null)
                await manager.RefreshAccountAsync();

            ApplyAccount();
            await LoadFriendsAsync();
            SetFeedback("Profile and friends are synced with Nakama.");
        }
        catch (Exception exception)
        {
            ApplyAccount();
            SetFeedback(GetErrorMessage(exception), true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    async System.Threading.Tasks.Task LoadFriendsAsync()
    {
        IApiFriendList result = await manager.ListFriendsAsync();
        friends.Clear();
        requests.Clear();

        if (result?.Friends != null)
        {
            foreach (IApiFriend friend in result.Friends)
            {
                if (friend?.User == null)
                    continue;

                FriendModel model = new(friend);

                if (friend.State == 0)
                    friends.Add(model);
                else if (friend.State == 1 || friend.State == 2)
                    requests.Add(model);
            }
        }

        RefreshFriends();
        RefreshRequests();
    }

    void RefreshFriends()
    {
        if (friendList == null)
            return;

        ClearChildren(friendList);
        string filter = friendSearchInput != null ? friendSearchInput.text.Trim() : string.Empty;
        int row = 0;

        foreach (FriendModel friend in friends)
        {
            if (!string.IsNullOrEmpty(filter) &&
                friend.DisplayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                friend.Username.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            CreateFriendRow(friendList, friend, row++, false);
        }
    }

    void RefreshRequests()
    {
        if (requestList == null)
            return;

        ClearChildren(requestList);

        for (int i = 0; i < requests.Count; i++)
            CreateFriendRow(requestList, requests[i], i, true);
    }

    void CreateFriendRow(Transform parent, FriendModel friend, int index, bool request)
    {
        GameObject row = CreateRect(parent, friend.Id, index % 2 == 0 ? RaisedColor : PanelColor);
        RectTransform rect = row.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -index * 58f);
        rect.sizeDelta = new Vector2(0f, 54f);

        TMP_Text name = CreateText(row.transform, "Name", friend.DisplayName, 15f, TextColor, FontStyles.Bold);
        SetAnchored(name.rectTransform, 0f, 0.45f, 0.62f, 1f, 12f, 0f, 0f, -4f);

        string detail = request ? (friend.IsIncoming ? "INCOMING" : "OUTGOING") : (friend.Online ? "ONLINE" : "OFFLINE");
        TMP_Text status = CreateText(row.transform, "Status", detail, 11f, friend.Online || request ? AccentColor : MutedTextColor);
        SetAnchored(status.rectTransform, 0f, 0f, 0.62f, 0.45f, 12f, 4f, 0f, 0f);

        if (request)
        {
            if (friend.IsIncoming)
            {
                Button accept = TrackBackendControl(CreateButton(row.transform, "Accept", "OK", AccentColor, () => AcceptRequest(friend)));
                SetAnchored(accept.GetComponent<RectTransform>(), 0.64f, 0.18f, 0.8f, 0.82f, 0f, 0f, -4f, 0f);

                Button decline = TrackBackendControl(CreateButton(row.transform, "Decline", "X", DangerColor, () => DeleteRequest(friend)));
                SetAnchored(decline.GetComponent<RectTransform>(), 0.82f, 0.18f, 0.98f, 0.82f, 0f, 0f, 0f, 0f);
            }
            else
            {
                Button cancel = TrackBackendControl(CreateButton(row.transform, "Cancel", "CANCEL", DangerColor, () => DeleteRequest(friend)));
                SetAnchored(cancel.GetComponent<RectTransform>(), 0.7f, 0.18f, 0.98f, 0.82f, 0f, 0f, 0f, 0f);
                cancel.GetComponentInChildren<TMP_Text>().fontSize = 11f;
            }
        }
        else
        {
            Button remove = TrackBackendControl(CreateButton(row.transform, "Remove", "REMOVE", DangerColor, () => RemoveFriend(friend)));
            SetAnchored(remove.GetComponent<RectTransform>(), 0.7f, 0.18f, 0.98f, 0.82f, 0f, 0f, 0f, 0f);
            remove.GetComponentInChildren<TMP_Text>().fontSize = 11f;
        }
    }

    void RefreshProfileSummary()
    {
        if (profileSummaryLabel != null)
            profileSummaryLabel.text = displayName + "\nNAKAMA PROFILE";
    }

    void RefreshAccountSummary()
    {
        string email = manager?.Account?.Email;
        if (accountSummaryLabel != null)
            accountSummaryLabel.text = "EMAIL: " + MaskEmail(email ?? string.Empty) + "  /  @" + username;
    }

    void OnAccountChanged()
    {
        ApplyAccount();
    }

    void ApplyAccount()
    {
        if (manager == null)
            return;

        displayName = manager.DisplayName;
        username = manager.Username;

        if (displayNameInput != null)
            displayNameInput.text = displayName;

        if (profilePreviewName != null)
            profilePreviewName.text = displayName.ToUpperInvariant();

        RefreshProfileSummary();
        RefreshAccountSummary();
    }

    T TrackBackendControl<T>(T control) where T : Selectable
    {
        control.interactable = !busy;
        backendControls.Add(control);
        return control;
    }

    void SetBusy(bool value)
    {
        busy = value;
        backendControls.RemoveAll(control => control == null);

        for (int i = 0; i < backendControls.Count; i++)
            backendControls[i].interactable = !value;

        RefreshAccountSummary();
    }

    void SetFeedback(string message, bool error = false)
    {
        if (feedbackLabel == null)
            return;

        feedbackLabel.text = message;
        feedbackLabel.color = error ? DangerColor : MutedTextColor;
    }

    static string MaskEmail(string email)
    {
        int separator = email.IndexOf('@');

        if (separator <= 0)
            return email;

        string local = email[..separator];
        string maskedLocal = local.Length <= 1 ? "*" : local[0] + new string('*', Math.Min(3, local.Length - 1));
        return maskedLocal + email[separator..];
    }

    static string GetErrorMessage(Exception exception)
    {
        if (exception == null || string.IsNullOrWhiteSpace(exception.Message))
            return "The Nakama request failed.";

        string message = exception.Message.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return message.Length <= 180 ? message : message[..180] + "...";
    }

    void FitWindowToCanvas()
    {
        if (windowRect == null)
            return;

        RectTransform canvasRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        float availableWidth = canvasRect != null ? canvasRect.rect.width - 48f : 960f;
        float availableHeight = canvasRect != null ? canvasRect.rect.height - 48f : 620f;
        windowRect.sizeDelta = new Vector2(Mathf.Min(960f, availableWidth), Mathf.Min(620f, availableHeight));
    }

    static GameObject CreateRect(Transform parent, string name, Color color)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = parent.gameObject.layer;
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = color.a > 0f;
        return gameObject;
    }

    static TMP_Text CreateText(Transform parent, string name, string value, float size, Color color, FontStyles style = FontStyles.Normal)
    {
        GameObject gameObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = parent.gameObject.layer;
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
        GameObject gameObject = CreateRect(parent, name, color);
        Button button = gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.55f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.onClick.AddListener(action);

        TMP_Text text = CreateText(gameObject.transform, "Label", label, 14f, PanelColor, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        Stretch(text.rectTransform, 6f);
        return button;
    }

    static TMP_InputField CreateInput(Transform parent, string name, string value, string placeholderValue)
    {
        GameObject root = CreateRect(parent, name, FieldColor);
        TMP_InputField input = root.AddComponent<TMP_InputField>();

        GameObject viewport = CreateRect(root.transform, "Text Area", Color.clear);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect, 12f, 8f);
        viewport.AddComponent<RectMask2D>();

        TMP_Text placeholder = CreateText(viewport.transform, "Placeholder", placeholderValue, 14f, MutedTextColor);
        placeholder.fontStyle = FontStyles.Italic;
        Stretch(placeholder.rectTransform);

        TMP_Text text = CreateText(viewport.transform, "Text", value, 14f, TextColor);
        Stretch(text.rectTransform);

        input.textViewport = viewportRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.text = value;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.caretColor = AccentColor;
        input.selectionColor = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.35f);
        return input;
    }

    static void CreateFieldLabel(Transform parent, string label, float left, float top, float rightAnchor)
    {
        TMP_Text text = CreateText(parent, label, label, 13f, WarmColor, FontStyles.Bold);
        SetAnchored(text.rectTransform, 0f, 1f, rightAnchor, 1f, left, top - 28f, -12f, top);
    }

    static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
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

    sealed class FriendModel
    {
        public FriendModel(IApiFriend friend)
        {
            Id = friend.User.Id;
            Username = friend.User.Username ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(friend.User.DisplayName)
                ? Username
                : friend.User.DisplayName;
            Online = friend.User.Online;
            State = friend.State;
        }

        public string Id { get; }
        public string Username { get; }
        public string DisplayName { get; }
        public bool Online { get; }
        public int State { get; }
        public bool IsIncoming => State == 2;
    }
}
