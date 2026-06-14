using UnityEngine;

/// <summary>
/// TODO[ANNOUNCE] Tạm dừng — đẩy Canvas UI đặc biệt lên trên bằng overrideSorting.
/// Gắn trên cùng GameObject với Canvas Screen Space - Camera.
/// </summary>
[DisallowMultipleComponent]
public class UITopLayerSupport : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Canvas targetCanvas;

    [Header("Sorting")]
    [SerializeField] private int baseSortingOrder = 100;

    public Canvas TargetCanvas => targetCanvas;

    void Awake()
    {
        // TODO[ANNOUNCE] ResolveCanvas + BringToFront khi bật lại.
    }

    void OnEnable()
    {
    }

    /// <summary>TODO[ANNOUNCE] Bật panel và đẩy canvas lên trước.</summary>
    public void Show(GameObject ui)
    {
    }

    /// <summary>TODO[ANNOUNCE] Ẩn panel.</summary>
    public void Hide(GameObject ui)
    {
    }

    /// <summary>TODO[ANNOUNCE] Đẩy canvas target lên trên các canvas khác.</summary>
    public void BringToFront()
    {
    }

    /// <summary>TODO[ANNOUNCE] Utility tĩnh — BringCanvasToFront(canvas).</summary>
    public static void BringCanvasToFront(Canvas canvas, int baseSortingOrder = 100)
    {
    }

    static void ApplyTopSorting(Canvas canvas, int baseSortingOrder)
    {
        int highest = baseSortingOrder;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas other = canvases[i];

            if (other == null || other == canvas)
                continue;

            if (other.overrideSorting && other.sortingOrder >= highest)
                highest = other.sortingOrder + 1;
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = highest;
    }

    bool ResolveCanvas()
    {
        if (targetCanvas != null)
            return true;

        if (TryGetComponent(out targetCanvas))
            return true;

        targetCanvas = GetComponentInParent<Canvas>();

        if (targetCanvas == null)
            Debug.LogWarning("[UITopLayerSupport] Không tìm thấy Canvas. Gắn script lên object có Canvas.", this);

        return targetCanvas != null;
    }

    [ContextMenu("Bring Canvas To Front")]
    void BringToFrontContextMenu()
    {
        UIAutoBindUtility.RecordUndo(this, "Bring Canvas To Front");
        BringToFront();
        UIAutoBindUtility.SetDirty(this);
    }
}
