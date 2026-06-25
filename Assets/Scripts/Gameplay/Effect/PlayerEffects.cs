using PurrNet;
using System.Linq;
using UnityEngine;

public class PlayerEffects : NetworkBehaviour
{
    public SyncList<ActiveEffect> effects = new();
    public SyncQueue<int> mosEfffectsId = new();
    void Update()
    {
        if (!isServer)
            return;
        if (effects.Count <= 0) return;
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];

            effect.remainingTime -= Time.deltaTime;

            if (effect.remainingTime <= 0)
            {
                RemoveEffect(effect);

                effects.RemoveAt(i);
            }
            else
            {
                effects[i] = effect;
            }
        }
    }
    // Nhưng món ko nằm trong đây được coi là hiệu ứng vĩnh viễn
    public void RemoveEffect(ActiveEffect effect)
    {
        var template = EffectDatabase.Instance.Get(effect.effectId);
        var player = GetComponent<PlayerInfor>();
        switch (template.effectType)
        {
            case EffectType.Speed:
                player.AddSpeed(-effect.specialValue);
                break;
            case EffectType.Shield:
                player.AddShield(-(int)effect.specialValue);
                break;
            case EffectType.MossTrap:
                RemoveEarliestMossTrapSkill();
                break;
            case EffectType.Strength: 
                break;

        }
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
    }

    // TODO[ANNOUNCE] Gọi PlayerBoardHub.LocalAnnounce khi effect mới được sync.
    void RefreshUI(SyncListChange<ActiveEffect> change)
    {
    }

    protected override void OnDespawned()
    {
        base.OnDespawned();
        // TODO[ANNOUNCE] effects.onChanged -= RefreshUI
    }

    public void AddEffect(EffectTemplate template)
    {
        if (!isServer)
            return;

        ActiveEffect effect = new ActiveEffect()
        {
            effectId = template.effectId,
            remainingTime = template.duration,
            specialValue = template.specialValue,
        };

        effects.Add(effect);

        ApplyEffect(effect);
        NotifyPlaceableBoardChanged();
    }

    void ApplyEffect(ActiveEffect effect)
    {
        var template = EffectDatabase.Instance.Get(effect.effectId);
        var player = GetComponent<PlayerInfor>();
        if (PlayerBoardHub.Instance != null)
            PlayerBoardHub.Instance.LocalAnnounce(template.description);
        switch (template.effectType)
        {
            case EffectType.Speed:
                player.AddSpeed(effect.specialValue);
                break;
            case EffectType.Shield:
                player.AddShield((int)effect.specialValue);
                break;
            case EffectType.MossTrap:
                mosEfffectsId.Enqueue(effect.effectId); 
                break;
            case EffectType.Crytal:
                player.AddGold((int)effect.specialValue); 
                break;
            case EffectType.Gold:
                player.AddGold((int)effect.specialValue); 
                break;
            case EffectType.BoomStack:
                player.AddBombCapacity((int)effect.specialValue);   
                break; 
        }
    }

    /// <summary>
    /// Kiểm tra player có đang active effect theo loại không.
    /// Dùng cho server quyết định đặt bomb thường hay đặt bẫy đặc biệt.
    /// </summary>
    public bool TryGetActiveEffectTemplate(EffectType effectType, out EffectTemplate template)
    {
        template = null;

        if (EffectDatabase.Instance == null)
            return false;

        for (int i = 0; i < effects.Count; i++)
        {
            ActiveEffect activeEffect = effects[i];

            if (activeEffect.remainingTime <= 0f)
                continue;

            if (!EffectDatabase.Instance.TryGet(activeEffect.effectId, out EffectTemplate effectTemplate))
                continue;

            if (effectTemplate == null)
                continue;

            if (effectTemplate.effectType != effectType)
                continue;

            template = effectTemplate;
            return true;
        }

        return false;
    }
    public void RemoveEarliestMossTrapSkill() 
    {
        if (mosEfffectsId.Count < 1 || effects.Count <= 0 ) 
        {
            Debug.Log("Không có moss trap skill để xóa");
            return; 
        }
        ActiveEffect target = effects.list.First(effect => effect.effectId == mosEfffectsId.Peek());
        Debug.Log($"target.id = {target.effectId}"); 
        mosEfffectsId.Dequeue();
        if (effects.Remove(target)) 
        {
            Debug.Log("Số lượng moss trap còn lại: " + MossTrapSkillCount);
        }

        NotifyPlaceableBoardChanged();
    }
    public int MossTrapSkillCount => mosEfffectsId.Count;

    void NotifyPlaceableBoardChanged()
    {
        if (!isServer)
            return;

        if (TryGetComponent(out PlayerController controller))
            controller.RefreshBoardPlaceables();
    }
}
