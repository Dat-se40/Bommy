using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Compact in-game player name + backend connection indicator.
/// </summary>
public class BackendConnectionStatusChipUI : MonoBehaviour
{
    const string PlayerDisplayNamePrefName = "PlayerDisplayName";
    const float DefaultDotSize = 12f;
    const float DefaultLabelWidth = 138f;
    const float DefaultLabelHeight = 22f;
    const float DefaultLabelFontSize = 16f;
    const float MinimumDotSize = 6f;
    const float MinimumLabelWidth = 48f;
    const float MinimumLabelHeight = 14f;
    const float MinimumLabelFontSize = 10f;

    static Sprite dotSprite;

    [Header("UI")]
    [SerializeField] private TMP_Text playerNameLabel;
    [SerializeField] private Image statusDot;
    [SerializeField] private Image background;

    [Header("Colors")]
    [SerializeField] private Color disconnectedColor = new(0.48f, 0.5f, 0.52f, 1f);
    [SerializeField] private Color connectedColor = new(0.28f, 0.86f, 0.42f, 1f);
    [SerializeField] private Color failedColor = new(0.9f, 0.16f, 0.12f, 1f);
    [SerializeField] private Color backgroundColor = new(0.08f, 0.09f, 0.1f, 0.72f);
    [SerializeField] private Color labelColor = new(0.96f, 0.93f, 0.84f, 1f);

    [Header("Generated Layout")]
    [SerializeField] private TextAnchor childAlignment = TextAnchor.MiddleRight;
    [SerializeField] private int paddingLeft = 12;
    [SerializeField] private int paddingRight = 12;
    [SerializeField] private int paddingTop = 6;
    [SerializeField] private int paddingBottom = 6;
    [SerializeField] private float spacing = 8f;
    [SerializeField] private float dotSize = 12f;
    [SerializeField] private float labelWidth = 138f;
    [SerializeField] private float labelHeight = 22f;
    [SerializeField] private float labelFontSize = 16f;

    [Header("Editor Preview")]
    [SerializeField] private string editorPreviewName = "Player";

    [Header("Runtime")]
    [SerializeField] private bool ensureManagerOnEnable = true;
    [SerializeField] private bool autoBuildChildren = true;

    NakamaConnectionManager manager;
    NakamaConnectionStatus lastStatus = NakamaConnectionStatus.Uninitialized;
    bool lastServerReady;
    bool refreshRequested;

    void Awake()
    {
        EnsureVisuals();
    }

    void OnEnable()
    {
        EnsureVisuals();

        if (ensureManagerOnEnable)
        {
            manager = NakamaConnectionManager.EnsureExists();
            _ = manager.InitializeAsync();
        }
        else
        {
            manager = NakamaConnectionManager.Instance;
        }

        NakamaConnectionManager.StatusChanged += RequestRefresh;
        Refresh();
    }

    void Update()
    {
        NakamaConnectionManager currentManager = NakamaConnectionManager.Instance;
        NakamaConnectionStatus currentStatus = currentManager != null
            ? currentManager.Status
            : NakamaConnectionStatus.Uninitialized;
        bool currentServerReady = currentManager != null && currentManager.IsServerReady;

        if (refreshRequested || currentManager != manager || currentStatus != lastStatus || currentServerReady != lastServerReady)
            Refresh();
    }

    void OnDisable()
    {
        NakamaConnectionManager.StatusChanged -= RequestRefresh;
    }

    void Refresh()
    {
        refreshRequested = false;
        manager = NakamaConnectionManager.Instance;
        lastStatus = manager != null ? manager.Status : NakamaConnectionStatus.Uninitialized;
        lastServerReady = manager != null && manager.IsServerReady;

        if (playerNameLabel != null)
            playerNameLabel.text = ResolveDisplayName();

        if (statusDot != null)
            statusDot.color = ResolveDotColor();
    }

    void RequestRefresh()
    {
        refreshRequested = true;
    }

    void EnsureVisuals()
    {
        NormalizeLayoutValues();
        TryAutoBind();

        if (autoBuildChildren && (playerNameLabel == null || statusDot == null))
        {
            BuildGeneratedLayout();
            TryAutoBind();
        }

        ApplyStaticStyle();
    }

    string ResolveDisplayName()
    {
        if (manager != null && !string.IsNullOrWhiteSpace(manager.DisplayName))
            return manager.DisplayName;

        string playerPrefsName = PlayerPrefs.GetString(PlayerDisplayNamePrefName, string.Empty);

        if (!string.IsNullOrWhiteSpace(playerPrefsName))
            return playerPrefsName;

        return "Player";
    }

    Color ResolveDotColor()
    {
        if (manager == null)
            return disconnectedColor;

        if (manager.IsServerReady)
            return connectedColor;

        return manager.Status == NakamaConnectionStatus.Failed
            ? failedColor
            : disconnectedColor;
    }

    void BuildGeneratedLayout()
    {
        background = GetComponent<Image>();

        if (background == null)
            background = gameObject.AddComponent<Image>();

        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();

        if (layout == null)
            layout = gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.childAlignment = childAlignment;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.padding = BuildPadding();
        layout.spacing = spacing;

        if (statusDot == null)
            statusDot = CreateStatusDot(transform);

        if (playerNameLabel == null)
            playerNameLabel = CreateNameLabel(transform);
    }

    Image CreateStatusDot(Transform parent)
    {
        GameObject dotObject = new("StatusDot", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        dotObject.transform.SetParent(parent, false);

        RectTransform dotRect = dotObject.GetComponent<RectTransform>();
        dotRect.sizeDelta = new Vector2(dotSize, dotSize);

        LayoutElement layoutElement = dotObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = dotSize;
        layoutElement.preferredHeight = dotSize;
        layoutElement.minWidth = dotSize;
        layoutElement.minHeight = dotSize;

        Image image = dotObject.GetComponent<Image>();
        image.sprite = GetDotSprite();
        image.preserveAspect = true;
        image.color = disconnectedColor;

        return image;
    }

    TMP_Text CreateNameLabel(Transform parent)
    {
        GameObject labelObject = new("PlayerNameLabel", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(labelWidth, labelHeight);

        LayoutElement layoutElement = labelObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = labelWidth;
        layoutElement.minWidth = 72f;
        layoutElement.preferredHeight = labelHeight;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "Player";
        label.fontSize = labelFontSize;
        label.alignment = TextAlignmentOptions.MidlineRight;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.color = labelColor;

        return label;
    }

    void ApplyStaticStyle()
    {
        if (background == null)
            background = GetComponent<Image>();

        if (background != null)
            background.color = backgroundColor;

        if (playerNameLabel != null)
        {
            playerNameLabel.color = labelColor;
            playerNameLabel.fontSize = labelFontSize;
            playerNameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            playerNameLabel.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (statusDot != null)
        {
            statusDot.sprite = GetDotSprite();
            statusDot.preserveAspect = true;
            statusDot.color = disconnectedColor;
        }

        ApplyExistingLayoutChildren();
    }

    void ApplyExistingLayoutChildren()
    {
        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();

        if (layout != null)
        {
            layout.childAlignment = childAlignment;
            layout.padding = BuildPadding();
            layout.spacing = spacing;
        }

        if (statusDot != null)
        {
            RectTransform dotRect = statusDot.rectTransform;
            dotRect.sizeDelta = new Vector2(dotSize, dotSize);

            LayoutElement dotLayout = statusDot.GetComponent<LayoutElement>();

            if (dotLayout != null)
            {
                dotLayout.preferredWidth = dotSize;
                dotLayout.preferredHeight = dotSize;
                dotLayout.minWidth = dotSize;
                dotLayout.minHeight = dotSize;
            }
        }

        if (playerNameLabel != null)
        {
            RectTransform labelRect = playerNameLabel.rectTransform;
            labelRect.sizeDelta = new Vector2(labelWidth, labelHeight);

            LayoutElement labelLayout = playerNameLabel.GetComponent<LayoutElement>();

            if (labelLayout != null)
            {
                labelLayout.preferredWidth = labelWidth;
                labelLayout.preferredHeight = labelHeight;
            }
        }
    }

    RectOffset BuildPadding()
    {
        return new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
    }

    void NormalizeLayoutValues()
    {
        paddingLeft = Mathf.Max(0, paddingLeft);
        paddingRight = Mathf.Max(0, paddingRight);
        paddingTop = Mathf.Max(0, paddingTop);
        paddingBottom = Mathf.Max(0, paddingBottom);

        dotSize = NormalizeSizeValue(dotSize, MinimumDotSize, DefaultDotSize);
        labelWidth = NormalizeSizeValue(labelWidth, MinimumLabelWidth, DefaultLabelWidth);
        labelHeight = NormalizeSizeValue(labelHeight, MinimumLabelHeight, DefaultLabelHeight);
        labelFontSize = NormalizeSizeValue(labelFontSize, MinimumLabelFontSize, DefaultLabelFontSize);
    }

    static float NormalizeSizeValue(float value, float minimum, float fallback)
    {
        if (value <= 1f)
            return fallback;

        return Mathf.Max(minimum, value);
    }

    static Sprite GetDotSprite()
    {
        if (dotSprite != null)
            return dotSprite;

        const int size = 32;
        const float radius = size * 0.5f - 1f;
        Vector2 center = new(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32 clear = new(255, 255, 255, 0);
        Color32 white = new(255, 255, 255, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? white : clear);
            }
        }

        texture.Apply();

        dotSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
        dotSprite.hideFlags = HideFlags.HideAndDontSave;

        return dotSprite;
    }

    void TryAutoBind()
    {
        if (playerNameLabel == null)
        {
            playerNameLabel = UIAutoBindUtility.FindChildComponent<TMP_Text>(
                this,
                "PlayerNamelbl",
                "PlayerNameLbl",
                "PlayerNameLabel",
                "PlayerName"
            );
        }

        if (statusDot == null)
        {
            statusDot = UIAutoBindUtility.FindChildComponent<Image>(
                this,
                "StatusDot",
                "ConnectionDot",
                "BackendStatusDot"
            );
        }

        if (background == null)
            background = GetComponent<Image>();
    }

    void OnValidate()
    {
        NormalizeLayoutValues();

        TryAutoBind();
        ApplyStaticStyle();

        if (!Application.isPlaying && playerNameLabel != null)
            playerNameLabel.text = string.IsNullOrWhiteSpace(editorPreviewName) ? "Player" : editorPreviewName;
    }
}
