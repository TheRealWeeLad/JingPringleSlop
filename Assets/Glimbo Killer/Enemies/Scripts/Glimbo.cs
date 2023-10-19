using UnityEngine;

public class Glimbo : Enemy
{
    // Default Properties
    public override string Name { get; protected set; } = "Glimbo";
    public override float MAXHEALTH { get; } = 100;
    public override float Damage { get; } = 1;
    public override float Speed { get; protected set; } = 8f;
    public override uint MoneyReward { get; protected set; } = 10;
    [field: SerializeField]
    public override Transform Target { get; protected set; }

    protected override void Attack()
    {
        throw new System.NotImplementedException();
    }
}
