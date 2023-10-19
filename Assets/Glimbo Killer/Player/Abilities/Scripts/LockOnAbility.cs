using System.Collections.Generic;
using UnityEngine;

public abstract class LockOnAbility : Ability
{
    public override FireMode FiringMode { get; } = FireMode.HOLD;

    LineRenderer _lineRenderer;
    Transform _camera;
    readonly List<Vector3> positions = new();

    Vector3 _compareFrom;

    // ABILITY BOUNCE
    protected abstract bool ShouldBounce { get; }
    protected virtual float BounceRadius { get; } = 5f;
    protected abstract LayerMask BounceLayers { get; set; }
    const ushort MAXBOUNCES = 5;
    readonly List<Transform> entitiesHit = new();

    // BOUNCY BABY
    float time = 0;
    const float BOUNCEAMPLITUDE = 0.1f;
    const float BOUNCESPEED = 5f;
    protected abstract float JITTER { get; }

    // CONE DETECTION
    const float RANGE = 15f;
    const float XCONEANGLE = Mathf.PI / 4;
    const float YCONEANGLE = Mathf.PI / 5;
    const float DISTBETWEENPOSITIONS = Mathf.PI / 3;

    // ABILITY TICKS
    protected const ushort ticks = 10;
    int _lastTime = 0;
    bool _hitThisFrame = false;

    private void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _camera = Camera.main.transform;
        _compareFrom = _camera.position;
    }

    private void Update()
    {
        _lineRenderer.positionCount = 0;
        positions.Clear();
        entitiesHit.Clear();
        _hitThisFrame = false;

        Transform target = FindClosestTarget(EnemyManager.EnemyList.ConvertAll(x => x.transform));
        if (target == null) { ResetEverything(); return; };

        Vector3 fromPos = _camera.position + _camera.right; // Offset Line
        Hit(fromPos, target, 0);

        _lineRenderer.SetPositions(positions.ToArray());
        if (_hitThisFrame) _lastTime = (int)(time * 100);
        time += Time.deltaTime;
    }

    void Hit(Vector3 fromPos, Transform target, int bounces)
    {
        if (bounces == MAXBOUNCES) return;

        Vector3 direction = target.position - fromPos;
        float distToEnemy = direction.magnitude;
        direction.Normalize();
        int numPositions = (int)(distToEnemy / DISTBETWEENPOSITIONS) + 1; // Round Up

        for (int i = 0; i < numPositions - 1; i++)
        {
            float xComp = DISTBETWEENPOSITIONS * i;
            float yComp = (Mathf.Sin(xComp + time % (2 * Mathf.PI) * BOUNCESPEED) + Random.Range(-JITTER, JITTER)) * BOUNCEAMPLITUDE;
            positions.Add(fromPos + xComp * direction + yComp * _camera.up);
        }
        positions.Add(target.position);

        // Deal Ability Effect every tick
        int bigTime = (int)(time * 100);
        if (bigTime - 100 / ticks >= _lastTime)
        {
            DealAbilityEffect(target);
            _hitThisFrame = true;
        }

        _lineRenderer.positionCount += numPositions;
        entitiesHit.Add(target);

        if (ShouldBounce)
        {
            Transform next = FindNextTarget(target.position);
            if (next != null) Hit(target.position, next, bounces + 1);
        }
    }

    void ResetEverything()
    {
        time = 0;
        _lastTime = 0;
    }

    bool InsideCone(Vector3 targetPos)
    {
        // Check if target within range and camera pointing at it for simple cases at close range
        Physics.Raycast(_camera.position, _camera.forward, out RaycastHit hitinfo, RANGE);
        if (hitinfo.collider != null && hitinfo.collider.gameObject.CompareTag("Enemy")) return true;

        // Elliptical cone with x and y as func of z
        Vector3 diff = targetPos - _camera.position; // Treat camera pos as origin
        float effectiveZ = Vector3.Dot(diff, _camera.forward); // Get effective Z value to calculate X and Y
        if (effectiveZ < 0) return false; // pos is behind camera
        if (effectiveZ > RANGE) return false; // too far
        float deltaX = effectiveZ * Mathf.Sin(XCONEANGLE / 2);
        float deltaY = effectiveZ * Mathf.Sin(YCONEANGLE / 2);
        // Check if pos inside ellipse with a=deltaX and b=deltaY and center _camera.position
        // y <= sqrt(b^4(a^2 - x^2))
        float effectiveX = Vector3.Dot(diff, _camera.right);
        if (Mathf.Abs(effectiveX) > deltaX) return false; // X value not in range
        float maxY = Mathf.Sqrt(Mathf.Pow(deltaY, 4) * (Mathf.Pow(deltaX, 2) - Mathf.Pow(effectiveX, 2)));
        float effectiveY = Vector3.Dot(diff, _camera.up);
        if (Mathf.Abs(effectiveY) > maxY) return false;

        return true;
    }

    Transform FindClosestTarget(List<Transform> targets)
    {
        targets.Sort(CompareTransform);
        foreach (Transform t in targets)
        {
            if (InsideCone(t.position)) return t;
        }
        return null;
    }

    int CompareTransform(Transform t1, Transform t2)
    {
        float dist1 = (_compareFrom - t1.position).magnitude;
        float dist2 = (_compareFrom - t2.position).magnitude;
        return dist1.CompareTo(dist2);
    }

    Transform FindNextTarget(Vector3 from)
    {
        List<Collider> hits = new(Physics.OverlapSphere(from, BounceRadius, BounceLayers));
        if (hits.Count <= 1) return null; // ignore original target
        List<Transform> hitsTrans = hits.ConvertAll(x => x.transform);
        _compareFrom = from;
        hitsTrans.Sort(CompareTransform);

        for (int i = 1; i < hits.Count; i++)
        {
            Transform hit = hitsTrans[i];
            if (!entitiesHit.Contains(hit)) return hit;
        }
        return null;
    }

    protected abstract void DealAbilityEffect(Transform target);
}