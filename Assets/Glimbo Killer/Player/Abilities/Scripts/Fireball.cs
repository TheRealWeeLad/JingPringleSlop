using UnityEngine;

public class Fireball : DamagingProjectile
{
    public override float Damage { get; } = 30;
    public override float AOERadius { get; } = 1f;
    public override ushort Cooldown { get; } = 3;
    public override float Speed { get; } = 50f;
    public ushort BurnChance { get; } = 25;
    public ushort FireballLevel { get; } = 1;
    const ushort _burnStrength = 1;

    protected override void ApplyDamageEffect(Enemy enemy)
    {
        // Chance to Burn Enemy
        if (Random.Range(1, 100) <= BurnChance)
        {
            ushort burnStrength = (ushort)(_burnStrength * (ushort)Mathf.Pow(2, (FireballLevel - 1)));
            StatusEffectManager.ApplyEffect(StatusEffect.BURN, enemy, burnStrength);
        }
    }
}
