public class Bomb : ArcingProjectile
{
    public override ushort Cooldown { get; } = 5;
    public override float AOERadius { get; } = 3f;
    public override float ThrowForce { get; } = 30;
    protected override bool Damaging { get; } = true;
    public override int Damage { get; } = 50;

    protected override void ApplyDamageEffect(Enemy enemy)
    {
        // BLOW UP IG ???
    }
}
