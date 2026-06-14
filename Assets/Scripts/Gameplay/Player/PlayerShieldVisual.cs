using UnityEngine;

/// <summary>
/// Bật/tắt shield visual trên mọi client theo PlayerBoardState.shield (SyncVar).
/// Gắn trên GameObject Vfx dưới Visual của player.
/// </summary>
public class PlayerShieldVisual : MonoBehaviour
{
    [SerializeField] private GameObject shieldVisual;

    PlayerBoardState boardState;
    int lastDisplayedShield = -1;

    void Awake()
    {
        if (shieldVisual == null)
            shieldVisual = UIAutoBindUtility.FindChildGameObject(this, "Shield", "ShieldVisual", "Vfx");

        if (shieldVisual == null)
            shieldVisual = gameObject;

        SetShieldVisible(false);
    }

    void OnEnable()
    {
        if (boardState == null)
            boardState = GetComponentInParent<PlayerBoardState>();

        if (boardState != null)
            boardState.Changed += OnBoardStateChanged;
    }

    void OnDisable()
    {
        if (boardState != null)
            boardState.Changed -= OnBoardStateChanged;
    }

    void Start()
    {
        SnapshotShield();
    }

    void OnBoardStateChanged()
    {
        RefreshFromBoardState();
    }

    void SnapshotShield()
    {
        if (boardState == null)
            return;

        lastDisplayedShield = boardState.Shield;
        SetShieldVisible(lastDisplayedShield > 0);
    }

    void RefreshFromBoardState()
    {
        if (boardState == null)
            return;

        int shield = boardState.Shield;

        if (shield == lastDisplayedShield)
            return;

        SetShieldVisible(shield > 0);
        lastDisplayedShield = shield;
    }

    void SetShieldVisible(bool visible)
    {
        if (shieldVisual != null)
            shieldVisual.SetActive(visible);
    }
}
