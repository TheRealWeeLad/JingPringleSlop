using System.Collections.Generic;
using UnityEngine;

public abstract class ProjectileAbility : Ability
{
    public abstract float Speed { get; }
    public override FireMode FiringMode { get; } = FireMode.PRESS;
    public GameObject DeathEffectPrefab { get; protected set; }
    protected List<GameObject> HitEntities = new();
    Vector3 direction;
    bool _hitSomething = false;
    protected LayerMask _hitLayers;

    private void Awake() 
    {
        direction = Camera.main.transform.forward;
        _hitLayers = AbilityProperties.HitLayers;
    }

    private void Update()
    {
        if (UIManagement.IsPaused()) return;
        Move();
    }

    protected virtual void Move()
    {
        transform.Translate(Speed * Time.deltaTime * direction);
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (_hitSomething) return;
        if ((_hitLayers & (1 << other.gameObject.layer)) == 0) return; // Hit layer not in allowed layers
        _hitSomething = true;

        AbilityController.StaleAbilities.Add(this);
        OnDeath(other);
    }

    public void SetDeathEffect(GameObject effect)
    {
        DeathEffectPrefab = effect;
    }

    protected virtual void OnDeath(Collider hit)
    {
        Quaternion spawnRot = Quaternion.Euler(-90f, 0, 0);
        GameObject deathEffect = Instantiate(DeathEffectPrefab, transform.position, spawnRot);
        deathEffect.GetComponent<DeathEffect>().SetHitObject(hit.transform);

        // Check sphere of AOE for hits
        Collider[] hits = Physics.OverlapSphere(transform.position, AOERadius, _hitLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            HitEntities.Add(hits[i].gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position, AOERadius);
    }
}