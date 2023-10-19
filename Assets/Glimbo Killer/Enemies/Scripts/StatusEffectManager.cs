using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;

public enum StatusEffect { BURN };

public class StatusEffectManager
{
    public static Dictionary<StatusEffect, IStatusEffect> StatusEffects = new()
    {
        { StatusEffect.BURN, new DOTEffect(5, 1f) }
    };

    static readonly List<Sprite> _icons = new(Resources.LoadAll<Sprite>("Status"));
    public static Dictionary<StatusEffect, Sprite> StatusIcons = _icons.Zip(StatusEffects.Keys, (sprite, status) => new { sprite, status })
                                                                    .ToDictionary(x => x.status, x => x.sprite);

    public static void ApplyEffect(StatusEffect effectName, Enemy enemy, ushort effectStrength)
    {
        IStatusEffect effect = StatusEffects[effectName];

        IEnumerator coroutine = default;

        ushort appliedStrength =  (ushort)(effectStrength + enemy.StatusStrength(effectName));

        if (effect is DOTEffect dotEffect)
        {
            coroutine = DealDOT(dotEffect, appliedStrength, enemy);
        }
        
        if (enemy.HasStatus(effectName)) enemy.ReplaceCoroutine(effectName, coroutine);
        else enemy.StartCoroutine(coroutine);

        // Add Status in Enemy code
        enemy.AddStatus(effectName, effectStrength, coroutine);
    }
    static IEnumerator DealDOT(DOTEffect effect, uint strength, Enemy enemy)
    {
        for (; ;)
        {
            yield return new WaitForSeconds(effect.TimeBetweenTicks);
            enemy.TakeDamage(effect.Damage * strength);
        }
    }
}

public interface IStatusEffect { }

public readonly struct DOTEffect : IStatusEffect
{
    public readonly uint Damage { get; }
    public readonly float TimeBetweenTicks { get; } // Time in milliseconds

    public DOTEffect(uint dmg, float time)
    {
        Damage = dmg;
        TimeBetweenTicks = time;
    }
}