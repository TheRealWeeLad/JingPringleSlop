using System.Collections.Generic;
using UnityEngine;

public abstract class ArcingProjectile : ProjectileAbility
{
    public override float Speed { get; } = 3f;
    public override FireMode FiringMode { get; } = FireMode.HOLDANDRELEASE;
    public abstract float ThrowForce { get; }
    protected abstract bool Damaging { get; }
    public abstract int Damage { get; }

    public Material aoeMaterial;
    public Material pathMaterial;

    public List<Vector3> positions;

    Rigidbody _rb;
    ProjectilePath _pathScript;

    float _time = 0;
    Vector3 _x0;
    Vector3 _v0;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        // Get x0 and v0
        Transform cam = Camera.main.transform;
        _v0 = cam.forward * ThrowForce / _rb.mass;
        _x0 = cam.position;
    }

    void FixedUpdate()
    {
        Vector3 newPos = ProjectilePath.GetPositionAtTime(_x0, _v0, _time);
        Vector3 dir = (newPos - transform.position).normalized;

        // Check for Skipped Collision
        if (Physics.Raycast(transform.position, dir, out RaycastHit hit, dir.magnitude, _hitLayers)) {
            OnTriggerEnter(hit.collider);
        }

        transform.position = newPos;
        
        _time += Time.fixedDeltaTime * Speed;
    }

    // Show Projectile Path
    public void Aim()
    {
        _rb = GetComponent<Rigidbody>();

        GameObject lrObject = new("Prediction Path");
        _pathScript = lrObject.AddComponent<ProjectilePath>();

        _pathScript.Aim(lrObject, aoeMaterial, pathMaterial, ThrowForce, _rb.mass, AOERadius);
    }

    public void StopAiming(ArcingProjectile ability)
    {
        if (!_pathScript) return;
        _pathScript.StopAiming();

        ability.positions = _pathScript.positions;
    }

    protected override void OnDeath(Collider hit)
    {
        base.OnDeath(hit);

        if (Damaging)
        {
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
    }

    protected abstract void ApplyDamageEffect(Enemy enemy);
}
