using UnityEngine;

public abstract class DamagingProjectile : ProjectileAbility
{
    public abstract float Damage { get; }

    protected override void OnDeath(Collider hit)
    {
        base.OnDeath(hit);

        // Deal Damage
        foreach (GameObject entity in HitEntities)
        {
            if (entity.TryGetComponent(out Enemy enemy))
            {
                enemy.TakeDamage(Damage);
                ApplyDamageEffect(enemy);
            }
        }
        HitEntities.Clear();
    }

    protected abstract void ApplyDamageEffect(Enemy enemy); }
