using System.Collections;
using PurrNet;
using UnityEngine;

/// <summary>
/// Bẫy rêu được đặt thay bomb thường khi player đang có buff MossTrap.
/// Sau khi được kích hoạt, player khác bước vào sẽ mất HP và bẫy biến mất.
/// </summary>
public class MossTrapController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D triggerCollider;
    [SerializeField] private Animator animator;

    [Header("Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float defaultArmDelay = 0.4f;
    [SerializeField] private int defaultDamage = 1;
    [SerializeField] private bool defaultIgnoreOwner = true;

    private PlayerController owner;
    private EffectTemplate effect;

    private bool armed;
    private bool triggered;

    private void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider2D>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.enabled = false;
        }
    }

    public void Init(PlayerController owner, EffectTemplate effect)
    {
        this.owner = owner;
        this.effect = effect;

        StartCoroutine(ArmAfterDelay());
    }

    /// <summary>
    /// Chờ một khoảng ngắn trước khi bẫy có thể gây sát thương.
    /// </summary>
    private IEnumerator ArmAfterDelay()
    {
        float delay = effect != null ? effect.fuseSeconds : defaultArmDelay;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        armed = true;

        if (triggerCollider != null)
            triggerCollider.enabled = true;

        if (animator != null)
            animator.SetBool("Armed", true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTrigger(other);
    }

    /// <summary>
    /// Kích hoạt bẫy nếu collider bước vào là player hợp lệ.
    /// </summary>
    private void TryTrigger(Collider2D other)
    {
        if (!armed || triggered || other == null)
            return;

        if (!IsInPlayerLayer(other.gameObject))
            return;

        PlayerController target = other.GetComponentInParent<PlayerController>();

        if (target == null)
            return;

        if (ShouldIgnoreOwner(target))
            return;

        TriggerTrap(target);
    }

    private bool IsInPlayerLayer(GameObject target)
    {
        return (playerLayer.value & (1 << target.layer)) != 0;
    }

    private bool ShouldIgnoreOwner(PlayerController target)
    {
        bool ignoreOwner = effect != null ? effect.ignoreOwner : defaultIgnoreOwner;

        if (!ignoreOwner)
            return false;

        return owner != null && target == owner;
    }

    /// <summary>
    /// Server xử lý damage và xóa bẫy để tránh lệch trạng thái multiplayer.
    /// </summary>
    private void TriggerTrap(PlayerController target)
    {
        triggered = true;

        if (triggerCollider != null)
            triggerCollider.enabled = false;

        if (!isServer)
            return;

        int damage = effect != null ? effect.damage : defaultDamage;

        ApplyDamage(target, damage);
        SpawnVfx();

        // Trả lại slot bomb cho người đặt.
        if (owner != null)
            owner.OnBombExploded();

        Destroy(gameObject);
    }

    /// <summary>
    /// Gọi hàm trừ HP của player. Đổi đoạn này theo health system thật của project.
    /// </summary>
    private void ApplyDamage(PlayerController target, int damage)
    {
        target.gameObject.SendMessageUpwards(
            "TakeDamage",
            damage,
            SendMessageOptions.DontRequireReceiver
        );
    }

    private void SpawnVfx()
    {
        if (effect == null || effect.vfxPrefab == null)
            return;

        Instantiate(effect.vfxPrefab, transform.position, Quaternion.identity);
    }
}
