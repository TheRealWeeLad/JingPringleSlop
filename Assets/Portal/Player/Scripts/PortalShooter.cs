using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;

public enum CSGFunc { Union, Subtract, Intersect}

public class PortalShooter : MonoBehaviour
{
    [Header("Portal Properites")]
    [Range(0, 2)]
    public float portalWidth = 1.0f;
    [Range(0, 3)]
    public float portalHeight = 1.75f;
    [Range(50, 500)]
    public float maxDistance = 100f;
    [Range(0, 1f)]
    public float portalColliderThickness = 0.1f;
    public LayerMask hitMask;
    public GameObject portalPrefab;
    public Material redMat;
    public Material blueMat;
    public Material linkedMat;

    Vector2 _portalDims;
    readonly Material[] _portalMats = new Material[2];
    readonly int[] _portalSurfaceLayers = new int[2];
    readonly GameObject[] _lastHitSurfaces = new GameObject[2];
    readonly GameObject[] _originalSurfaces = new GameObject[2];
    readonly GameObject[] _editedSurfaces = new GameObject[2];

    [Space()]
    public const float shootCooldown = 0.5f;
    float _timeSinceShot = 1.0f;

    const float OFFSET = 0.001f;

    Transform _mainCam;
    readonly Portal[] _portals = new Portal[2];

    private void Awake()
    {
        _mainCam = Camera.main.transform;
        _portalMats[0] = redMat;
        _portalMats[1] = blueMat;
        _portalDims = new(portalWidth, portalHeight);
    }

    private void Update()
    {
        _timeSinceShot += Time.deltaTime;
    }

    public void Shoot(bool red)
    {
        if (_timeSinceShot < shootCooldown || UIManagement.IsPaused()) return;

        if (Physics.Raycast(_mainCam.position, _mainCam.forward, out RaycastHit hit, maxDistance, hitMask))
        {
            int isRed = red ? 0 : 1;

            // Spawn Portal
            SpawnPortal(isRed, hit);
        }

        _timeSinceShot = 0;
    }

    void SpawnPortal(int isRed, RaycastHit hit)
    {
        // Offset portal if it would clip through ground/ceiling/whatever
        Vector3 spawnPoint = CalculateSpawnPoint(hit, hit.transform);

        GameObject portal = InitializePortal(isRed, spawnPoint);
        Mesh mesh = portal.GetComponent<MeshFilter>().mesh;
        Portal p = portal.GetComponent<Portal>();

        ResetPortalAndSurface(isRed);
        _portals[isRed] = p;

        // Set Vertices based on hit object rotation
        List<Vector3> vertices = new();
        Vector3 normal = hit.normal;

        Vector3 right = Vector3.right * portalWidth;
        Vector3 up = Vector3.up * portalHeight;
        vertices.Add(-right + up);
        vertices.Add(right + up);
        vertices.Add(right - up);
        vertices.Add(-right - up);

        mesh.SetVertices(vertices);

        // Set collider size
        BoxCollider collider = portal.GetComponent<BoxCollider>();
        Vector3 forward = Vector3.Cross(right, up);
        forward.Normalize();
        // Width = right * 2, Height = up * 2
        collider.size = right * 2 + up * 2 + forward * portalColliderThickness;
        // Offset by half width to prevent collisions from behind
        collider.center = forward * portalColliderThickness * 0.5f;

        // Set triangles BACKWARD to make them visible
        int[] triangles = new int[6] { 2, 1, 0, 0, 3, 2 };
        mesh.triangles = triangles;

        // Set normals BACKWARD
        mesh.SetNormals(new Vector3[4] { -normal, -normal, -normal, -normal });

        // Set uvs
        Vector2[] uvs = new Vector2[4] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
        mesh.uv = uvs;

        // Update Portal Materials and Directions
        Vector3 relativeRight = Vector3.Cross(normal, Vector3.up).normalized * portalWidth;
        // Special Case if hit object is parallel to ground
        if (relativeRight == Vector3.zero)
        {
            relativeRight = _mainCam.right * portalWidth;
        }
        Vector3 relativeUp = Vector3.Cross(relativeRight, normal).normalized * portalHeight;
        up.Normalize();
        p.SetDirections(normal, relativeUp);
        for (int i = 0; i < _portals.Length; i++)
        {
            Portal port = _portals[i];
            if (port == null) continue;
            port.GetComponent<MeshRenderer>().material = p.IsLinked() ? linkedMat : _portalMats[i];
        }

        // Temporarily change layer for collision
        GameObject hitObject = hit.collider.gameObject;
        // If we hit the same object twice, use last version of object as the hitObject
        GameObject lastHit = _lastHitSurfaces[isRed];
        if (lastHit != null)
        {
            _lastHitSurfaces[isRed] = hitObject; // Reassign
            if (hitObject.transform.position == lastHit.transform.position)
                hitObject = lastHit;
        }
        _portalSurfaceLayers[isRed] = hitObject.layer;

        // Take portal-shaped chunk out of hit surface to walk through
        ChangeGeometry(isRed, hitObject, hit.point, hit.normal);

        hitObject.layer = LayerMask.NameToLayer("PortalSurface");
    }


    Vector3 CalculateSpawnPoint(RaycastHit hit, Transform hitObject)
    {
        Vector3 spawn = hit.point;
        Vector3 normal = hit.normal;
        Vector3 relativeRight = Vector3.Cross(normal, Vector3.up).normalized * portalWidth;
        // Special Case if hit object is parallel to ground
        if (relativeRight == Vector3.zero)
            relativeRight = _mainCam.right * portalWidth;
        Vector3 relativeUp = Vector3.Cross(relativeRight, normal).normalized * portalHeight;

        Bounds objectBounds = hitObject.GetComponent<Collider>().bounds;
        Vector3 farBound = Vector3.Max(objectBounds.max, objectBounds.min);
        Vector3 nearBound = Vector3.Min(objectBounds.max, objectBounds.min);
        // Check all corners
        (int, int)[] ops = { (1, 1), (1, -1), (-1, 1), (-1, -1) };
        for (int i = 0; i < 4; i++)
        {
            (int one, int two) = ops[i];
            Vector3 corner = spawn + one * relativeRight + two * relativeUp;

            Vector3 farDiff = farBound - corner;
            Vector3 nearDiff = corner - nearBound;
            spawn += Vector3.Min(farDiff, Vector3.zero) - Vector3.Min(nearDiff, Vector3.zero);
        }

        return spawn;
    }

    GameObject InitializePortal(int isRed, Vector3 spawnPoint)
    {
        GameObject portal = Instantiate(portalPrefab, spawnPoint - _mainCam.forward * OFFSET, Quaternion.identity);
        Portal p = portal.GetComponent<Portal>();
        // Initialize portal based on color
        p.Color = (PortalColor)isRed;
        int idx = _portals.Length - 1 - isRed;
        Portal otherPortal = _portals[idx];
        if (otherPortal != null)
        {
            p.Link(otherPortal);
            otherPortal.Link(p);
        }
        portal.GetComponent<MeshRenderer>().material = _portalMats[isRed];

        return portal;
    }

    void ResetPortalAndSurface(int isRed)
    {
        // Delete previous portal and reset surface
        Portal previousPortal = _portals[isRed];
        if (previousPortal != null)
        {
            _originalSurfaces[isRed].SetActive(true);
            Destroy(_editedSurfaces[isRed]);
            _lastHitSurfaces[isRed].layer = _portalSurfaceLayers[isRed];
            Destroy(previousPortal.gameObject);
        }
    }

    void ChangeGeometry(int isRed, GameObject hitObj, Vector3 hitPoint, Vector3 hitNormal)
    {
        GameObject subCube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // Get Scale Values
        Vector3 scale = hitObj.transform.rotation * hitObj.transform.localScale;

        // Special Case if hit object is parallel to ground
        Vector3 right = Vector3.Cross(hitNormal, Vector3.up).normalized;
        if (right == Vector3.zero)
        {
            right = _mainCam.right;
            subCube.transform.rotation = Quaternion.Euler(0f, _mainCam.rotation.eulerAngles.y, 0f);
        }
        subCube.transform.rotation *= hitObj.transform.rotation;
        Vector3 up = Vector3.Cross(right, hitNormal).normalized;

        // Scale direction vectors
        Vector3 rightScale = portalWidth * 2 * right;
        Vector3 upScale = portalHeight * 2 * up;
        Vector3 forwardScale = Vector3.Dot(scale, hitNormal) * hitNormal;
        Vector3 newScale = Quaternion.Inverse(subCube.transform.rotation) * (rightScale + upScale + forwardScale);
        newScale = new(Mathf.Abs(newScale.x), Mathf.Abs(newScale.y), Mathf.Abs(newScale.z));

        subCube.transform.localScale = newScale;

        subCube.transform.position = hitPoint + forwardScale.magnitude / 2 * -hitNormal;

        // Create new mesh from subtraction
        GameObject result = hitObj.PerformCSGFunc(subCube, CSGFunc.Subtract);
        _editedSurfaces[isRed] = result;
        // Hide original object
        result.SetActive(true);
        hitObj.SetActive(false);

        Destroy(subCube);
    }
}
