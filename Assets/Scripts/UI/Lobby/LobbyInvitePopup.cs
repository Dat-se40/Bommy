using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyInvitePopup : MonoBehaviour
{
    const string SingletonName = "[LobbyInvitePopup]";

    static LobbyInvitePopup instance;

    Canvas canvas;
    GameObject overlay;
    TMP_Text titleText;
    TMP_Text messageText;
    TMP_Text detailText;
    Button acceptButton;
    Button declineButton;
    Action acceptAction;
    Action declineAction;

    public static LobbyInvitePopup EnsureExists()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<LobbyInvitePopup>();

        if (instance != null)
            return instance;

        GameObject host = new(SingletonName);
        instance = host.AddComponent<LobbyInvitePopup>();
        return instance;
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUi();
        Hide();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void Show(LobbyInviteNotification invite, Action accept, Action decline)
    {
        if (invite == null)
            return;

        acceptAction = accept;
        declineAction = decline;

        string sender = string.IsNullOrWhiteSpace(invite.senderName) ? "A friend" : invite.senderName;
        string roomName = string.IsNullOrWhiteSpace(invite.roomName) ? "a lobby" : invite.roomName;
        string mapName = string.IsNullOrWhiteSpace(invite.mapName) ? "Unknown map" : invite.mapName;

        titleText.text = "Lobby Invite";
        messageText.text = sender + " invited you to " + roomName + ".";
        detailText.text = "Room " + invite.roomId + " - " + mapName;
        overlay.SetActive(true);
    }

    void Accept()
    {
        Action action = acceptAction;
        Hide();
        action?.Invoke();
    }

    void Decline()
    {
        Action action = declineAction;
        Hide();
        action?.Invoke();
    }

    void Hide()
    {
        acceptAction = null;
        declineAction = null;

        if (overlay != null)
            overlay.SetActive(false);
    }

    void BuildUi()
    {
        if (canvas != null)
            return;

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        gameObject.AddComponent<GraphicRaycaster>();

        overlay = CreateRect("Overlay", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.56f);

        RectTransform panel = CreateRect(
            "Panel",
            overlay.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f),
            new Vector2(520f, 260f)
        );
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.13f, 0.98f);

        titleText = CreateText("Title", panel, "Lobby Invite", 30, FontStyles.Bold, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -36f), new Vector2(-64f, 48f));
        messageText = CreateText("Message", panel, "", 22, FontStyles.Normal, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 22f), new Vector2(-64f, 72f));
        detailText = CreateText("Details", panel, "", 18, FontStyles.Normal, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -34f), new Vector2(-64f, 40f));

        acceptButton = CreateButton(panel, "AcceptButton", "Accept", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-90f, 48f));
        declineButton = CreateButton(panel, "DeclineButton", "Decline", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(90f, 48f));

        acceptButton.onClick.AddListener(Accept);
        declineButton.onClick.AddListener(Decline);
    }

    static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    static TMP_Text CreateText(string name, Transform parent, string text, int size, FontStyles style, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, anchoredPosition, new Vector2(140f, 44f));
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.23f, 0.21f, 1f);

        Button button = rect.gameObject.AddComponent<Button>();
        ButtonClickSound.EnsureOn(button);

        TMP_Text text = CreateText("Label", rect, label, 18, FontStyles.Bold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        text.raycastTarget = false;
        return button;
    }
}
