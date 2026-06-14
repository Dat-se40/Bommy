using DG.Tweening;
using UnityEngine;

/// <summary>
/// Hiệu ứng hình ảnh trên mọi client — driven bởi PlayerBoardState SyncVar.
/// Không đặt DOTween trong PlayerInfor (server-only).
/// </summary>
[RequireComponent(typeof(PlayerBoardState))]
public class PlayerVisualFeedback : MonoBehaviour
{
    [Header("Damage Feedback")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float hitFlashDuration = 0.45f;
    [SerializeField] private int hitFlashCount = 3;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Header("Death Feedback")]
    [SerializeField] private string inactiveLayerName = "Nothing";
    [SerializeField] private float deathArcHeight = 1.1f;
    [SerializeField] private float deathArcDuration = 0.55f;

    PlayerBoardState boardState;
    Sequence damageFlashSequence;
    Sequence deathSequence;

    SpriteRenderer[] spriteRenderers;
    Color[] originalSpriteColors;
    Collider2D[] colliders;
    Rigidbody2D rb;
    MovementController movementController;
    PlayerController playerController;

    int originalLayer;
    RigidbodyType2D originalBodyType;
    Vector3 visualBaseLocalPosition;
    Quaternion visualBaseLocalRotation;

    int lastDisplayedHp = -1;
    bool lastEliminated;
    bool isDeathVisualActive;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalLayer = gameObject.layer;

        if (rb != null)
            originalBodyType = rb.bodyType;

        boardState = GetComponent<PlayerBoardState>();
        CacheComponents();
    }

    void OnEnable()
    {
        if (boardState == null)
            boardState = GetComponent<PlayerBoardState>();

        if (boardState != null)
            boardState.Changed += OnBoardStateChanged;
    }

    void OnDisable()
    {
        if (boardState != null)
            boardState.Changed -= OnBoardStateChanged;

        StopDamageFlash();
        StopDeathFeedback(resetVisual: true);
    }

    void Start()
    {
        SnapshotBoardState();
    }

    void OnBoardStateChanged()
    {
        if (boardState == null)
            return;

        int hp = boardState.CurrentHp;
        bool eliminated = boardState.IsEliminated;

        if (lastDisplayedHp >= 0 && hp < lastDisplayedHp && hp > 0)
            PlayHitFlash();

        if (lastDisplayedHp > 0 && hp <= 0 && !eliminated)
            PlayDeathFeedback();

        if (lastDisplayedHp <= 0 && hp > 0 && !eliminated)
            PlayRespawnRestore();

        if (!lastEliminated && eliminated)
            PlayEliminated();

        lastDisplayedHp = hp;
        lastEliminated = eliminated;
    }

    void SnapshotBoardState()
    {
        if (boardState == null)
            return;

        lastDisplayedHp = boardState.CurrentHp;
        lastEliminated = boardState.IsEliminated;
    }

    void PlayHitFlash()
    {
        CacheComponents();

        if (spriteRenderers == null || spriteRenderers.Length == 0)
            return;

        StopDamageFlash();

        float flashStep = hitFlashDuration / Mathf.Max(hitFlashCount * 2, 1);
        damageFlashSequence = DOTween.Sequence();

        for (int i = 0; i < hitFlashCount; i++)
        {
            damageFlashSequence.AppendCallback(() => SetSpriteColors(hitFlashColor));
            damageFlashSequence.AppendInterval(flashStep);
            damageFlashSequence.AppendCallback(RestoreSpriteColors);
            damageFlashSequence.AppendInterval(flashStep);
        }
    }
    void PlayDeathFeedback()
    {
        if (isDeathVisualActive)
            return;

        CacheComponents();
        StopDamageFlash();

        isDeathVisualActive = true;

        SetPhysicsActive(false);
        SetLayerRecursively(gameObject, ResolveInactiveLayer());

        Transform target = GetVisualTarget();
        Vector3 landPosition = target.position + Vector3.down * 0.25f;

        StopDeathFeedback(resetVisual: false);

        deathSequence = DOTween.Sequence();
        deathSequence.Append(
            target
                .DOJump(landPosition, deathArcHeight, 1, deathArcDuration)
                .SetEase(Ease.OutQuad)
        );
        deathSequence.OnComplete(() => isDeathVisualActive = false);
    }

    void PlayRespawnRestore()
    {
        isDeathVisualActive = false;
        StopDeathFeedback(resetVisual: true);
        RestorePhysicsAndLayer();
        gameObject.SetActive(true);
    }

    void PlayEliminated()
    {
        isDeathVisualActive = false;
        StopDeathFeedback(resetVisual: true);
        RestorePhysicsAndLayer();
        gameObject.SetActive(false);
    }

    void CacheComponents()
    {
        if (movementController == null)
            movementController = GetComponent<MovementController>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider2D>(true);

        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (visualRoot == null && spriteRenderers != null && spriteRenderers.Length > 0)
            visualRoot = spriteRenderers[0].transform;

        if (originalSpriteColors == null || originalSpriteColors.Length != spriteRenderers.Length)
        {
            originalSpriteColors = new Color[spriteRenderers.Length];

            for (int i = 0; i < spriteRenderers.Length; i++)
                originalSpriteColors[i] = spriteRenderers[i].color;
        }

        if (visualRoot != null)
        {
            visualBaseLocalPosition = visualRoot.localPosition;
            visualBaseLocalRotation = visualRoot.localRotation;
        }
    }

    Transform GetVisualTarget()
    {
        return visualRoot != null ? visualRoot : transform;
    }

    void SetSpriteColors(Color color)
    {
        if (spriteRenderers == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            spriteRenderers[i].color = color;
        }
    }

    void RestoreSpriteColors()
    {
        if (spriteRenderers == null || originalSpriteColors == null)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null)
                continue;

            spriteRenderers[i].color = originalSpriteColors[i];
        }
    }

    void SetPhysicsActive(bool active)
    {
        if (rb != null)
        {
            if (!active)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                rb.bodyType = originalBodyType;
            }
        }

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = active;
            }
        }

        if (movementController != null)
            movementController.enabled = active;

        if (playerController != null)
            playerController.enabled = active;
    }

    void RestorePhysicsAndLayer()
    {
        SetPhysicsActive(true);
        SetLayerRecursively(gameObject, originalLayer);
    }

    void RestoreVisualRootTransform()
    {
        Transform target = GetVisualTarget();

        if (target == transform)
            return;

        target.localPosition = visualBaseLocalPosition;
        target.localRotation = visualBaseLocalRotation;
    }

    int ResolveInactiveLayer()
    {
        int layer = LayerMask.NameToLayer(inactiveLayerName);

        if (layer < 0)
        {
            Debug.LogWarning("[PlayerVisualFeedback] Layer '" + inactiveLayerName + "' not found. Falling back to Ignore Raycast.");
            layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        return layer;
    }

    static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null)
            return;

        root.layer = layer;

        Transform rootTransform = root.transform;

        for (int i = 0; i < rootTransform.childCount; i++)
            SetLayerRecursively(rootTransform.GetChild(i).gameObject, layer);
    }

    void StopDamageFlash()
    {
        damageFlashSequence?.Kill();
        damageFlashSequence = null;
        RestoreSpriteColors();
    }

    void StopDeathFeedback(bool resetVisual)
    {
        Transform target = GetVisualTarget();
        target.DOKill();

        deathSequence?.Kill();
        deathSequence = null;

        if (resetVisual)
            RestoreVisualRootTransform();
    }
}
