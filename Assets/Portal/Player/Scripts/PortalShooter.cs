using System.Collections.Generic;
using UnityEngine;
using Parabox.CSG;

public class PortalShooter : MonoBehaviour
{
    [Header("Portal Properites")]
    [Range(0, 2)]
    public float portalWidth = 1.0f;
    [Range(0, 3)]
    public float portalHeight = 1.75f;
    [Range(50, 500)]
    public float maxDistance = 100f;
    public LayerMask hitMask;
    public GameObject portalPrefab;
    public Material redMat;
    public Material blueMat;
    public Material linkedMat;

    readonly Material[] _portalMats = new Material[2];

    [Space()]
    public const float shootCooldown = 0.5f;
    float _timeSinceShot = 1.0f;

    const float OFFSET = 0.001f;

    Transform _mainCam;
    readonly Portal[] _portals = new Portal[2];

    readonly GameObject[] _hitObjects = new GameObject[2];
    readonly int[] _hitLayers = new int[3];

    enum CSGFunc
    {
        Subtract, Union, Intersect
    }

    private void Awake()
    {
        _mainCam = Camera.main.transform;
        _portalMats[0] = redMat;
        _portalMats[1] = blueMat;
    }

    private void Update()
    {
        _timeSinceShot += Time.deltaTime;
    }

    public void Shoot(bool red)
    {
        if (_timeSinceShot < shootCooldown) return;

        if (Physics.Raycast(_mainCam.position, _mainCam.forward, out RaycastHit hit, maxDistance, hitMask))
        {
            int isRed = red ? 0 : 1;
            // Remove part of hit mesh that portal replaces
            // ChangeGeometry(isRed, hit); GRINGLE SPRINGLE POOKIE BEAR

            // Spawn Portal
            SpawnPortal(isRed, hit);
        }

        _timeSinceShot = 0;
    }

    void ChangeGeometry(int isRed, RaycastHit hit)
    {
        GameObject hitObj = hit.collider.gameObject;
        GameObject subCube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // Get Scale Values
        Vector3 scale = hit.transform.rotation * hit.transform.localScale;

        Vector3 normal = hit.normal;
        // Special Case if hit object is parallel to ground
        Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
        if (right == Vector3.zero)
        {
            right = _mainCam.right;
            subCube.transform.rotation = Quaternion.Euler(0f, _mainCam.rotation.eulerAngles.y, 0f);
        }
        subCube.transform.rotation *= hit.transform.rotation;
        Vector3 up = Vector3.Cross(right, normal).normalized;

        // Scale direction vectors
        Vector3 rightScale = portalWidth * 2 * right;
        Vector3 upScale = portalHeight * 2 * up;
        Vector3 forwardScale = Vector3.Dot(scale, normal) * normal;
        Vector3 newScale = Quaternion.Inverse(subCube.transform.rotation) * (rightScale + upScale + forwardScale);
        newScale = new(Mathf.Abs(newScale.x), Mathf.Abs(newScale.y), Mathf.Abs(newScale.z));

        subCube.transform.localScale = newScale;

        subCube.transform.position = hit.point + forwardScale.magnitude / 2 * -normal;

        // Create new mesh from subtraction
        PerformCSGFunc(CSGFunc.Subtract, hitObj, subCube);

        // Hide original object
        Destroy(subCube);
        Destroy(hitObj);
    }

    void SpawnPortal(int isRed, RaycastHit hit)
    {
        GameObject portal = Instantiate(portalPrefab, hit.point - _mainCam.forward * OFFSET, Quaternion.identity);
        Mesh mesh = portal.GetComponent<MeshFilter>().mesh;
        Portal p = portal.GetComponent<Portal>();
        // Initialize portal based on color
        p.color = (PortalColor)isRed;
        int idx = _portals.Length - 1 - isRed;
        Portal otherPortal = _portals[idx];
        if (otherPortal != null)
        {
            p.Link(otherPortal);
            otherPortal.Link(p);
        }
        portal.GetComponent<MeshRenderer>().material = _portalMats[isRed];

        // Delete previous portal
        Portal previousPortal = _portals[isRed];
        if (previousPortal != null)
        {
            Destroy(previousPortal.gameObject);
        }

        _portals[isRed] = p;

        // Set Vertices based on hit object rotation
        List<Vector3> vertices = new();
        Vector3 normal = hit.normal;
        Vector3 right;

        // Special Case if hit object is parallel to ground
        right = Vector3.Cross(normal, Vector3.up).normalized * portalWidth;
        if (right == Vector3.zero)
        {
            right = _mainCam.right * portalWidth;
        }
        Vector3 up = Vector3.Cross(right, normal).normalized * portalHeight;

        vertices.Add(-right + up);
        vertices.Add(right + up);
        vertices.Add(right - up);
        vertices.Add(-right - up);

        mesh.SetVertices(vertices);

        // Set triangles
        int[] triangles = new int[6] { 0, 1, 2, 2, 3, 0 };
        mesh.triangles = triangles;

        // Set normals
        mesh.SetNormals(new Vector3[4] { normal, normal, normal, normal });

        // Set uvs
        Vector2[] uvs = new Vector2[4] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
        mesh.uv = uvs;

        // Update Portal Materials and Directions
        p.SetDirections(normal, up.normalized, right.normalized);
        for (int i = 0; i < _portals.Length; i++)
        {
            Portal port = _portals[i];
            if (port == null) continue;
            port.GetComponent<MeshRenderer>().material = p.IsLinked() ? linkedMat : _portalMats[i];
            // port.GetComponent<MeshRenderer>().material = _portalMats[i]; FOR DEBUG
        }

        // Reset layer of previously hit object
        GameObject lastHit = _hitObjects[isRed];
        if (lastHit != null)
        {
            lastHit.layer = _hitLayers[isRed];
        }
        // Camera should see through obj
        int layer = LayerMask.NameToLayer("PortalSurface" + isRed);
        portal.GetComponentInChildren<Camera>().cullingMask |= 1 << layer;
        
        GameObject hitObj = hit.collider.gameObject;
        _hitObjects[isRed] = hitObj;
        _hitLayers[isRed] = hitObj.layer;
        int bothLayer = LayerMask.NameToLayer("PortalSurfaceBoth");
        if (hitObj.layer == layer || hitObj.layer == bothLayer)
        {
            hitObj.layer = bothLayer;
        }
        else hitObj.layer = layer;

        // Render Portal
        PortalRenderPass.portals[isRed] = p;
    }

    /// <summary>
    /// Perform a CSG function based on its index
    /// </summary>
    /// <param name="funcIdx"></param>
    /// <param name="lhs">object to perform function on</param>
    /// <param name="rhs">object to subtract/add/intersect</param>
    /// <returns>Result GameObject</returns>
    GameObject PerformCSGFunc(CSGFunc func, GameObject lhs, GameObject rhs)
    {
        Model result;
        switch (func)
        {
            case CSGFunc.Subtract:
                result = CSG.Subtract(lhs, rhs);
                break;
            case CSGFunc.Union:
                result = CSG.Union(lhs, rhs);
                break;
            case CSGFunc.Intersect:
                result = CSG.Intersect(lhs, rhs);
                break;
            default:
                Debug.LogWarning(string.Format("CSG Function {0} not found", func));
                return null;
        }

        GameObject resultObj = new(string.Format("{0} Result", func));
        resultObj.AddComponent<MeshFilter>().sharedMesh = result.mesh;
        resultObj.AddComponent<MeshRenderer>().sharedMaterials = result.materials.ToArray();
        resultObj.AddComponent<MeshCollider>();
        resultObj.layer = lhs.layer;

        return resultObj;
    }
}
