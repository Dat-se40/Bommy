using System.Collections;
using PurrNet;
using UnityEngine;

/// <summary>
/// Bẫy rêu — detect player bằng OverlapBox (giống ExplosionCreator), server gây damage qua MatchGameplayAuthority.
/// Chỉ kích hoạt với người khác người đặt (so sánh PlayerID).
/// </summary>
public class MossTrapController : NetworkBehaviour
{
    #region Variables

    const float CellOverlapSize = 0.85f;
    static readonly Vector2 CellOverlapExtents = new Vector2(CellOverlapSize, CellOverlapSize);

    [Header("References")]
    [SerializeField] private Collider2D triggerCollider;
    [SerializeField] private Animator animator;

    [Header("Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float defaultArmDelay = 0.4f;
    [SerializeField] private int defaultDamage = 1;
    [SerializeField] private bool defaultIgnoreOwner = true;

    PlayerController trapOwner;
    EffectTemplate effect;
    PlayerID? ownerPlayerId;

    bool armed;
    bool triggered;
    bool slotReleased;
    bool isPlacedTrap;

    #endregion

    #region Unity Methods

    void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider2D>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void FixedUpdate()
    {
        if (!isPlacedTrap || !isServer || !armed || triggered)
            return;

        TryTriggerPlayersAtCell();
    }

    protected override void OnDestroy()
    {
        if (isPlacedTrap)
            ReleaseTrapSlot();

        base.OnDestroy();
    }

    #endregion

    #region Public Methods

    public void Init(PlayerController owner, EffectTemplate effect, Vector3Int bombCell)
    {
        isPlacedTrap = true;
        trapOwner = owner;
        this.effect = effect;

        if (trapOwner != null && trapOwner.owner.HasValue)
            ownerPlayerId = trapOwner.owner.Value;

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.enabled = false;
        }

        StartCoroutine(ArmAfterDelay());
    }

    #endregion

    #region Private Methods

    IEnumerator ArmAfterDelay()
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

    /// <summary>
    /// Cùng pattern ExplosionCreator.DamagePlayersAtCell — overlap ô bẫy, lấy PlayerInfor từ collider player.
    /// </summary>
    void TryTriggerPlayersAtCell()
    {
        MatchGameplayAuthority authority = MatchGameplayAuthority.Instance;

        if (authority == null)
        {
            FlowGuard.Error(
                FlowGuard.TagNetwork,
                "MatchGameplayAuthority missing — moss trap cannot apply damage.",
                this
            );
            return;
        }

        Vector3 center = transform.position;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, CellOverlapExtents, 0f, playerLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
                continue;

            if (!hit.TryGetComponent(out PlayerInfor injured))
                continue;

            PlayerID? injuredId = injured.GetPlayerID();

            if (!injuredId.HasValue)
                continue;

            if (ShouldSkipPlayer(injuredId.Value))
                continue;

            TriggerTrap(injuredId.Value, authority);
            return;
        }
    }

    bool ShouldSkipPlayer(PlayerID injuredId)
    {
        bool ignoreOwner = effect != null ? effect.ignoreOwner : defaultIgnoreOwner;

        if (!ignoreOwner)
            return false;

        return ownerPlayerId.HasValue && injuredId == ownerPlayerId.Value;
    }

    void TriggerTrap(PlayerID injuredId, MatchGameplayAuthority authority)
    {
        triggered = true;

        if (triggerCollider != null)
            triggerCollider.enabled = false;

        int damageAmount = effect != null ? effect.damage : defaultDamage;
        PlayerID attacker = ownerPlayerId ?? PlayerID.Server;

        authority.SubmitAttack(
            new AttackDTO
            {
                attacker = attacker,
                injured = injuredId,
                damage = damageAmount,
            }
        );

        SpawnVfx();
        ReleaseTrapSlot();
        Destroy(gameObject);
    }

    void SpawnVfx()
    {
        if (effect == null || effect.vfxPrefab == null)
            return;

        Instantiate(effect.vfxPrefab, transform.position, Quaternion.identity);
        SoundManager.Instance.PlaySfx(SoundKey.SfxPlayerTakenTrap); 
    }

    void ReleaseTrapSlot()
    {
        if (slotReleased || trapOwner == null)
            return;

        slotReleased = true;

        if (isServer)
            trapOwner.OnTrapRemoved();
    }

    #endregion
}
