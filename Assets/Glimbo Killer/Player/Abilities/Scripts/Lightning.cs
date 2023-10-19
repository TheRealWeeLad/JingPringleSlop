using UnityEngine;

public class Lightning : LockOnAbility
{
    public int DPS { get; } = 10;
    protected override float JITTER { get; } = 0.8f;
    protected override bool ShouldBounce { get; } = true;
    protected override LayerMask BounceLayers { get; set; }

    private void Awake()
    {
        BounceLayers = LayerMask.GetMask("Enemy");
    }

    protected override void DealAbilityEffect(Transform enemyTrans)
    {
        enemyTrans.GetComponent<Enemy>().TakeDamage((float)DPS / ticks, 1f / ticks);
    }
}
