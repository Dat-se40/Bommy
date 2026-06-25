using PurrNet;using UnityEngine;
using DG.Tweening;

public class PickableBuff : NetworkBehaviour
{
    [SerializeField]
    EffectTemplate effectTemplate;

    [SerializeField]
    Transform visualRoot;

    [Header("Float & Pulse (DOTween)")]
    [SerializeField]
    float floatHeight = 0.15f;

    [SerializeField]
    float floatDuration = 0.8f;

    [SerializeField]
    float scalePulseMultiplier = 1.1f; // Độ bự tối đa khi đập nhịp (1.1 = to hơn 10%)

    Vector3 _baseLocalPosition;
    Vector3 _baseScale;
    bool _isPickedUp;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        _isPickedUp = false; // Đặt lại trạng thái khi được spawn/pool
        Transform target = visualRoot != null ? visualRoot : transform;
        _baseLocalPosition = target.localPosition;
        _baseScale = target.localScale;

        StartFloatingAnimation();
    }

    void StartFloatingAnimation()
    {
        Transform target = visualRoot != null ? visualRoot : transform;

        // 1. Hiệu ứng bay lên xuống
        target
            .DOLocalMoveY(_baseLocalPosition.y + floatHeight, floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // 2. Hiệu ứng nhịp đập (Scale Pulse) nhẹ nhàng
        target
            .DOScale(_baseScale * scalePulseMultiplier, floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    protected override void OnDespawned()
    {
        Transform target = visualRoot != null ? visualRoot : transform;
        target.DOKill(); // Dọn dẹp toàn bộ tween khi despawn

        base.OnDespawned();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Nếu không phải server hoặc đã bị nhặt rồi thì bỏ qua
        if (!isServer || _isPickedUp)
            return;

        if (!collision.TryGetComponent(out PlayerEffects playerEffects))
            playerEffects = collision.GetComponentInParent<PlayerEffects>();

        if (playerEffects == null)
            return;

        if (effectTemplate == null)
        {
            Debug.LogWarning($"{nameof(PickableBuff)}: effectTemplate chưa gán trên {name}.");
            return;
        }

        // Đánh dấu là đã nhặt để không bị dính trigger lần nữa trong lúc chờ animation
        _isPickedUp = true;
        Debug.Log("Đã nhặt: " + effectTemplate.name); 
        playerEffects.AddEffect(effectTemplate);

        PickupFxRpc();
    }

    [ObserversRpc(runLocally: true)]
    void PickupFxRpc()
    {
        SoundPlayback.PlaySynced(SoundKey.SfxPickup);
        
        // Sinh ra Particle/VFX rời (nếu có)
        if (effectTemplate != null && effectTemplate.vfxPrefab != null)
            Instantiate(effectTemplate.vfxPrefab, transform.position, Quaternion.identity);
        if (effectTemplate.effectType == EffectType.MossTrap) 
        {
            if (isServer)
            {
                Destroy(gameObject);
            }
            return; 
        }
        Transform target = visualRoot != null ? visualRoot : transform;

        // Cần dừng animation lơ lửng/pulse hiện tại để không bị đụng độ với animation nhặt
        target.DOKill();

        // Tạo Sequence cho hiệu ứng nhặt
        Sequence pickupSeq = DOTween.Sequence();

        // 1. Bay lên 1.5 đơn vị so với vị trí hiện tại
        pickupSeq.Join(target.DOMoveY(target.position.y + 1.5f, 0.4f).SetEase(Ease.OutQuad));

        // 2. Phóng to một chút (tạo cảm giác "bật" lên)
        pickupSeq.Join(target.DOScale(_baseScale * 1.3f, 0.2f).SetEase(Ease.OutQuad));

        // 3. Sau khi phóng to thì thu nhỏ về 0 để biến mất
        pickupSeq.Append(target.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));

        // CHÚ Ý: Sau khi animation kết thúc mới gọi hàm Destroy.
        // Chỉ để Server thực hiện việc Destroy (Despawn) qua PurrNet.
        pickupSeq.OnComplete(() =>
        {
            if (isServer)
            {
                Destroy(gameObject);
            }
        });
    }
    public static PickableBuff Spawn(EffectTemplate template, Vector2 worldPosition)
    {
        if (template == null || template.pickupPrefab == null) 
        {
            Debug.Log("Missing something");
            return null;
        }
            

        var instance = Instantiate(
            template.pickupPrefab,
            worldPosition,
            Quaternion.identity);
        
        if (instance.TryGetComponent(out PickableBuff pickable)) 
        {
            Debug.Log("Success create" + nameof(pickable));
            return pickable;
        }
        

        Debug.LogWarning($"{nameof(PickableBuff)}: pickupPrefab thiếu {nameof(PickableBuff)} component.");
        return null;
    }
}