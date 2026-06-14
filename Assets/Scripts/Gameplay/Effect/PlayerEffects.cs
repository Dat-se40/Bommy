using PurrNet;
using UnityEngine;

public class PlayerEffects : NetworkBehaviour
{
    public SyncList<ActiveEffect> effects = new();
    void Update()
    {
        if (!isServer)
            return;

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

    void RemoveEffect(ActiveEffect effect)
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
        }
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        // TODO[ANNOUNCE] effects.onChanged += RefreshUI khi bật lại announcement.
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
    }

    void ApplyEffect(ActiveEffect effect)
    {
        var template = EffectDatabase.Instance.Get(effect.effectId);
        var player = GetComponent<PlayerInfor>();
        switch (template.effectType)
        {
            case EffectType.Speed:
                player.AddSpeed(effect.specialValue);
                break;
            case EffectType.Shield:
                player.AddShield((int)effect.specialValue);
                break;
        }
    }
}
