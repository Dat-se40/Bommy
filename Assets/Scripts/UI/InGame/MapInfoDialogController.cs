using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Hiển thị thông tin item/buff của map — singleton, toggle bằng InputActionReference (phím E).
/// Host GameObject phải luôn active; ẩn/hiện bằng CanvasGroup.
/// Prep: tự mở bảng khi vào phase, E / Closebtn bật tắt trong lúc Prep.
/// </summary>
public class MapInfoDialogController : MonoBehaviour
{
    #region Singleton

    static MapInfoDialogController instance;

    public static MapInfoDialogController Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance = FindAnyObjectByType<MapInfoDialogController>(FindObjectsInactive.Include);
            return instance;
        }
    }

    #endregion

    #region Variables

    [Header("Root")]
    [SerializeField] private GameObject overlayRoot;

    [Header("Buttons")]
    [SerializeField] private Button closebtn;

    [Header("Labels")]
    [SerializeField] private TMP_Text mapInfoTitlelbl;
    [SerializeField] private TMP_Text mapNamelbl;

    [Header("Items")]
    [SerializeField] private Transform itemList;
    [SerializeField] private MapInfoItemCardUI itemCardTemplate;
    [SerializeField] private GameObject prefabItemCard;
    [SerializeField] private int maxCards = 8;

    [Header("Fallback")]
    [SerializeField] private Sprite fallbackIcon;
    [SerializeField] private string emptyText = "No item data";

    [Header("Keyboard Actions")]
    [SerializeField] private InputActionReference toggleMapInfoAction;
    [SerializeField] private InputActionAsset gameInputActions;

    readonly List<MapInfoItemCardUI> spawnedCards = new();
    InputAction resolvedToggleAction;
    MatchPhaseKind lastAutoOverlayPhase = MatchPhaseKind.None;
    CanvasGroup overlayCanvasGroup;
    bool overlayVisible;
    bool allowToggleInPrep;

    #endregion

    #region Properties

    public bool IsOpen => overlayVisible;
    public bool CanToggle => allowToggleInPrep;

    #endregion

    #region Unity Methods

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"{nameof(MapInfoDialogController)}: duplicate on '{name}'.", this);
            return;
        }

        instance = this;
        ResolveOverlayReferences();

        if (closebtn != null)
        {
            closebtn.onClick.RemoveAllListeners();
            closebtn.onClick.AddListener(Close);
        }

        SetOverlayVisible(false);
        allowToggleInPrep = false;
    }

    void Start()
    {
        BindToggleInput();
        SubscribeMatchPhase();
    }

    void OnDestroy()
    {
        UnbindToggleInput();
        UnsubscribeMatchPhase();

        if (instance == this)
            instance = null;
    }

    #endregion

    #region Public Methods

    public void OpenFromCurrentLevel()
    {
        LevelConfig level = LevelRuntime.Current;

        if (level == null)
        {
            OpenEmpty();
            return;
        }

        SetOverlayVisible(true);

        if (mapInfoTitlelbl != null)
            mapInfoTitlelbl.text = "MAP INFO";

        if (mapNamelbl != null)
            mapNamelbl.text = level.displayName;

        RenderItems(level);
    }

    public void Close()
    {
        SetOverlayVisible(false);
    }

    public void Toggle()
    {
        if (!allowToggleInPrep)
            return;

        if (IsOpen)
            Close();
        else
            OpenFromCurrentLevel();
    }

    /// <summary>Prep bắt đầu — mở bảng + cho phép E / Closebtn toggle.</summary>
    public void BeginPrepPhase()
    {
        allowToggleInPrep = true;
        OpenFromCurrentLevel();
    }

    /// <summary>Hết Prep — đóng overlay, khóa E toggle.</summary>
    public void EndPrepPhase()
    {
        allowToggleInPrep = true;
        Close();
    }

    #endregion

    #region Private Methods

    void ResolveOverlayReferences()
    {
        if (overlayRoot == null)
            overlayRoot = gameObject;

        overlayCanvasGroup = GetComponent<CanvasGroup>();

        if (overlayCanvasGroup == null)
            overlayCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        Transform dialog = transform.Find("MapInfoDialog");

        if (dialog != null)
            dialog.gameObject.SetActive(true);
    }

    void SetOverlayVisible(bool visible)
    {
        overlayVisible = visible;

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = visible ? 1f : 0f;
            overlayCanvasGroup.interactable = visible;
            overlayCanvasGroup.blocksRaycasts = visible;
        }

        if (visible)
            transform.SetAsLastSibling();
    }

    void BindToggleInput()
    {
        resolvedToggleAction = ResolveToggleAction();

        if (resolvedToggleAction == null)
        {
            Debug.LogWarning(
                $"{nameof(MapInfoDialogController)}: chưa gán MapInfoToggle (InputActionReference hoặc Game Input asset).",
                this
            );
            return;
        }

        resolvedToggleAction.Enable();
        resolvedToggleAction.performed += OnToggleMapInfoPerformed;
    }

    void UnbindToggleInput()
    {
        if (resolvedToggleAction == null)
            return;

        resolvedToggleAction.performed -= OnToggleMapInfoPerformed;
        resolvedToggleAction.Disable();
        resolvedToggleAction = null;
    }

    void SubscribeMatchPhase()
    {
        MatchPhaseBroadcast broadcast = MatchPhaseBroadcast.Instance;

        if (broadcast == null)
            return;

        broadcast.PhaseChanged += HandleMatchPhaseForOverlay;
        HandleMatchPhaseForOverlay();
    }

    void UnsubscribeMatchPhase()
    {
        MatchPhaseBroadcast broadcast = MatchPhaseBroadcast.Instance;

        if (broadcast == null)
            return;

        broadcast.PhaseChanged -= HandleMatchPhaseForOverlay;
    }

    void OnToggleMapInfoPerformed(InputAction.CallbackContext context)
    {
        if (!allowToggleInPrep)
            return;

        Toggle();
    }

    InputAction ResolveToggleAction()
    {
        if (toggleMapInfoAction != null && toggleMapInfoAction.action != null)
            return toggleMapInfoAction.action;

        if (gameInputActions != null)
            return gameInputActions.FindAction("MapInfoToggle", false);

        return null;
    }

    void HandleMatchPhaseForOverlay()
    {
        MatchPhaseBroadcast broadcast = MatchPhaseBroadcast.Instance;

        if (broadcast == null)
            return;

        MatchPhaseKind phase = broadcast.CurrentPhase;

        if (phase == MatchPhaseKind.Prep)
        {
            if (lastAutoOverlayPhase != MatchPhaseKind.Prep) 
                BeginPrepPhase();
        }
        else if (lastAutoOverlayPhase == MatchPhaseKind.Prep)
        {
            EndPrepPhase();
        }

        lastAutoOverlayPhase = phase;
    }

    void RenderItems(LevelConfig level)
    {
        ClearCards();

        if (itemList == null || !HasItemCardSource())
        {
            Debug.LogWarning($"{nameof(MapInfoDialogController)}: thiếu Item List hoặc Item Card Template.", this);
            return;
        }

        DropEntry[] entries = level.destructibleDrops.entries;

        if (entries == null || entries.Length == 0)
        {
            SpawnEmptyCard();
            return;
        }

        HashSet<int> usedEffectIds = new();
        int count = 0;

        for (int i = 0; i < entries.Length; i++)
        {
            DropEntry entry = entries[i];
            EffectTemplate effect = entry.effect;

            if (effect == null)
                continue;

            if (!usedEffectIds.Add(effect.effectId))
                continue;

            SpawnEffectCard(effect, entry.maxSpawnCount);
            count++;

            if (count >= maxCards)
                break;
        }

        if (count == 0)
            SpawnEmptyCard();
    }

    MapInfoItemCardUI SpawnItemCard()
    {
        if (itemCardTemplate != null)
            return Instantiate(itemCardTemplate, itemList);

        if (prefabItemCard == null)
            return null;

        GameObject instance = Instantiate(prefabItemCard, itemList);

        if (!instance.TryGetComponent(out MapInfoItemCardUI card))
        {
            Debug.LogWarning(
                $"{nameof(MapInfoDialogController)}: prefabItemCard thiếu {nameof(MapInfoItemCardUI)}.",
                this
            );
            Destroy(instance);
            return null;
        }

        return card;
    }

    bool HasItemCardSource() => itemCardTemplate != null || prefabItemCard != null;

    void SpawnEffectCard(EffectTemplate effect, int maxSpawnCount)
    {
        MapInfoItemCardUI card = SpawnItemCard();

        if (card == null)
            return;

        card.gameObject.SetActive(true);

        string description = !string.IsNullOrWhiteSpace(effect.mapInfoDescription)
            ? effect.mapInfoDescription
            : effect.description;

        if (maxSpawnCount > 0)
        {
            if (string.IsNullOrWhiteSpace(description))
                description = $"Max spawn on map: {maxSpawnCount}";
            else
                description += $"\nMax spawn on map: {maxSpawnCount}";
        }

        card.Setup(
            effect.uiIcon != null ? effect.uiIcon : ResolveFallbackIcon(effect),
            effect.displayName,
            description
        );

        spawnedCards.Add(card);
    }

    Sprite ResolveFallbackIcon(EffectTemplate effect)
    {
        if (effect == null || effect.pickupPrefab == null)
            return fallbackIcon;

        SpriteRenderer spriteRenderer = effect.pickupPrefab.GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null && spriteRenderer.sprite != null)
            return spriteRenderer.sprite;

        return fallbackIcon;
    }

    void SpawnEmptyCard()
    {
        MapInfoItemCardUI card = SpawnItemCard();

        if (card == null)
            return;

        card.gameObject.SetActive(true);
        card.Setup(fallbackIcon, "ITEMS", emptyText);
        spawnedCards.Add(card);
    }

    void OpenEmpty()
    {
        SetOverlayVisible(true);

        if (mapInfoTitlelbl != null)
            mapInfoTitlelbl.text = "MAP INFO";

        if (mapNamelbl != null)
            mapNamelbl.text = "Unknown Map";

        ClearCards();
        SpawnEmptyCard();
    }

    void ClearCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
                Destroy(spawnedCards[i].gameObject);
        }

        spawnedCards.Clear();
    }

    #endregion
}
