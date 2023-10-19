using UnityEngine;

public class DeathEffect : MonoBehaviour
{
    public Transform hitObject;
    Vector3 _lastHitPos;

    void Update()
    {
        if (hitObject == null) return;

        transform.position += hitObject.position - _lastHitPos;

        _lastHitPos = hitObject.position;
    }

    public void SetHitObject(Transform hit)
    {
        hitObject = hit;
        _lastHitPos = hit.position;
    }
}
