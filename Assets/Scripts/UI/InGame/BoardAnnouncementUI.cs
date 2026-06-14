using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// TODO[ANNOUNCE] Tạm dừng — panel announcement slide lên ~2 ô grid khi có message.
/// </summary>
public class BoardAnnouncementUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform announcementPanel;
    [SerializeField] private TMP_Text announcementText;

    [Header("Slide")]
    [SerializeField] private float slideCellCount = 2f;
    [SerializeField] private float slidePixelsPerCell = 80f;
    [SerializeField] private Grid grid;

    [Header("Timing")]
    [SerializeField] private float slideInDuration = 0.35f;
    [SerializeField] private float showDuration = 2f;
    [SerializeField] private float slideOutDuration = 0.3f;

    [Header("Top Layer")]
    [SerializeField] private UITopLayerSupport topLayerSupport;

    Vector2 hiddenAnchoredPosition;
    Vector2 shownAnchoredPosition;
    Sequence announceSequence;
    bool initialized;

    void Awake()
    {
        TryAutoBind();

        if (topLayerSupport == null)
            topLayerSupport = GetComponentInParent<UITopLayerSupport>();

        InitializePositions();
    }

    void OnDisable()
    {
        announceSequence?.Kill();
        announceSequence = null;
    }

    public void Show(string message)
    {
        // TODO[ANNOUNCE] Bật lại slide DOTween + UITopLayerSupport.BringToFront.
    }

    void TryAutoBind()
    {
        if (announcementPanel == null)
            announcementPanel = transform as RectTransform;

        if (announcementText == null)
            announcementText = UIAutoBindUtility.FindChildComponent<TMP_Text>(
                this,
                "Announcement",
                "AnnounceText",
                "Message"
            );
    }

    void InitializePositions()
    {
        if (announcementPanel == null || initialized)
            return;

        hiddenAnchoredPosition = announcementPanel.anchoredPosition;
        shownAnchoredPosition = hiddenAnchoredPosition + Vector2.up * ResolveSlideOffsetY();
        initialized = true;
    }

    float ResolveSlideOffsetY()
    {
        Grid sourceGrid = grid;

        if (sourceGrid == null && MapRefs.Instance != null)
            sourceGrid = MapRefs.Instance.GetComponentInParent<Grid>();

        if (sourceGrid == null)
            return slidePixelsPerCell * slideCellCount;

        float worldDelta = sourceGrid.cellSize.y * slideCellCount;
        Camera cam = ResolveUiCamera();

        if (cam == null)
            return slidePixelsPerCell * slideCellCount;

        Vector3 worldA = Vector3.zero;
        Vector3 worldB = Vector3.up * worldDelta;
        float screenDeltaY = cam.WorldToScreenPoint(worldB).y - cam.WorldToScreenPoint(worldA).y;

        Canvas rootCanvas = announcementPanel.GetComponentInParent<Canvas>();

        if (rootCanvas != null)
            return screenDeltaY / Mathf.Max(rootCanvas.scaleFactor, 0.01f);

        return screenDeltaY;
    }

    Camera ResolveUiCamera()
    {
        Canvas canvas = announcementPanel != null
            ? announcementPanel.GetComponentInParent<Canvas>()
            : null;

        if (canvas != null)
        {
            if (canvas.worldCamera != null)
                return canvas.worldCamera;

            return Camera.main;
        }

        return Camera.main;
    }
}
