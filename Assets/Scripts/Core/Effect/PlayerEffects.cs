using PurrNet;
using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerEffects : NetworkBehaviour
{
    public SyncList<ActiveEffect> effects = new();
    Player player;
 
    void Update()
    {
        if (!isServer)
            return;

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];

            effect.remainingTime -=
                Time.deltaTime;

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

    void RemoveEffect(
    ActiveEffect effect)
    {
        var template =
            EffectDatabase.Instance
                .Get(effect.effectId);

        //switch (template.effectType)
        //{
        //    case EffectType.Speed:

        //        player.moveSpeed.value -=
        //            effect.specialValue;

        //        break;

        //    case EffectType.Shield:

        //        player.shield.value -=
        //            (int)effect.specialValue;

        //        break;
        //}
    }

    protected override void OnSpawned()
    {
        player = GetComponent<Player>();

        effects.onChanged += RefreshUI;
    }

    private void RefreshUI(SyncListChange<ActiveEffect> change)
    {
        throw new NotImplementedException();
    }

    public void AddEffect(
    EffectTemplate template)
    {
        if (!isServer)
            return;

        ActiveEffect effect =
            new ActiveEffect()
            {
                effectId = template.effectId,
                remainingTime = template.duration,
                specialValue = template.specialValue
            };

        effects.Add(effect);

        ApplyEffect(effect);
    }
    void ApplyEffect(ActiveEffect effect)
    {
        var template =
            EffectDatabase.Instance
                .Get(effect.effectId);
        // Đây là cái lí do tạo sao dùng type nè :V
        //switch (template.effectType)
        //{
        //    case EffectType.Speed:

        //        player.moveSpeed.value +=
        //            effect.specialValue;

        //        break;

        //    case EffectType.Shield:

        //        player.shield.value +=
        //            (int)effect.specialValue;

        //        break;
        //}
    }
}

// Class Giả
class Player 
{
}