using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectilePath : MonoBehaviour
{
    GameObject _aoeSphere;
    GameObject _lrObject;
    Material _aoeMaterial;
    Transform _mainCam;

    Coroutine _aimCoroutine;

    public List<Vector3> positions = new();

    LayerMask _hitLayers = 0;

    const int MAXTIMEPREDICTED = 5;
    const float STEPSIZE = 0.01f;
    const float DEFAULTRADIUS = 0.5f;
    const float OFFSET = 0.2f;
    const float LINEWIDTH = 0.1f;

    float _aoeRadius;

    private void Awake()
    {
        _mainCam = Camera.main.transform;
        _hitLayers = AbilityProperties.HitLayers;
    }

    public void Aim(GameObject lrObject, Material aoeMaterial, Material pathMaterial, float throwForce, float mass, float AOERadius)
    {
        _lrObject = lrObject;
        _aoeMaterial = aoeMaterial;
        _aoeRadius = AOERadius;

        LineRenderer lr = lrObject.AddComponent<LineRenderer>();
        lr.widthCurve = new(new Keyframe(0, LINEWIDTH), new Keyframe(1, LINEWIDTH));
        lr.material = pathMaterial;
        Color lrColor = aoeMaterial.color;
        Gradient grad = new() { colorKeys = new GradientColorKey[] { new(lrColor, 0), new(lrColor, 1) } };
        lr.colorGradient = grad;

        _aimCoroutine = StartCoroutine(UpdateAim(throwForce, mass, lr));
    }

    IEnumerator UpdateAim(float throwForce, float mass, LineRenderer lr)
    {
        for (; ; )
        {
            List<Vector3> newPositions = new();

            // Simulate Throw
            Vector3 offset = _mainCam.right * OFFSET;
            Vector3 v0 = _mainCam.forward * throwForce / mass; // v0 = acceleration for Impulse Force
            Vector3 x0 = _mainCam.position + offset;

            float t = 0f;
            bool hitObject = false;
            for (int i = 0; t < MAXTIMEPREDICTED; i++)
            {
                // Predict position
                t = i * STEPSIZE;
                Vector3 position = GetPositionAtTime(x0, v0, t);
                newPositions.Add(position);
                Vector3 nextPos = GetPositionAtTime(x0, v0, t + STEPSIZE);
                Vector3 dir = nextPos - position;

                if (Physics.Raycast(position, dir.normalized, out RaycastHit hit, dir.magnitude, _hitLayers))
                {
                    // Render sphere of AOE
                    if (!_aoeSphere)
                    {
                        _aoeSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        Destroy(_aoeSphere.GetComponent<SphereCollider>()); // Remove Collider
                        MeshRenderer meshRenderer = _aoeSphere.GetComponent<MeshRenderer>();
                        meshRenderer.material = _aoeMaterial;

                        float scale = _aoeRadius / DEFAULTRADIUS;
                        _aoeSphere.transform.localScale = new(scale, scale, scale);
                    }
                    _aoeSphere.transform.position = hit.point;
                    hitObject = true;
                    break;
                }
            }

            if (!hitObject && _aoeSphere)
            {
                Destroy(_aoeSphere);
            }

            lr.positionCount = newPositions.Count;
            lr.SetPositions(newPositions.ToArray());
            positions = new(newPositions);

            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    public static Vector3 GetPositionAtTime(Vector3 x0, Vector3 v0, float time)
    {
        return x0 + v0 * time + 0.5f * time * time * Physics.gravity;
    }

    public void StopAiming()
    {
        StopCoroutine(_aimCoroutine);
        Destroy(_lrObject);
        Destroy(_aoeSphere);
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < positions.Count - 1; i++)
        {
            Vector3 pos1 = positions[i];
            Vector3 pos2 = positions[i + 1];

            Gizmos.DrawLine(pos1, pos2);
        }
    }
}