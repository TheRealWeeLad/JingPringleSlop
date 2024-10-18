using System.Collections.Generic;
using UnityEngine;

public class PortalShooter : MonoBehaviour
{
    [Header("Portal Properites")]
    [Range(0, 2)]
    public float portalWidth = 1.0f;
    [Range(0, 3)]
    public float portalHeight = 1.75f;
    [Range(50, 500)]
    public float maxDistance = 100f;
    [Range(0, 0.3f)]
    public float portalColliderThickness = 0.1f;
    public LayerMask hitMask;
    public GameObject portalPrefab;
    public Material redMat;
    public Material blueMat;
    public Material linkedMat;

    readonly Material[] _portalMats = new Material[2];
    readonly int[] _portalSurfaceLayers = new int[2];
    readonly GameObject[] _portalSurfaces = new GameObject[2];

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

            // Spawn Portal
            SpawnPortal(isRed, hit);
        }

        _timeSinceShot = 0;
    }

    //void ChangeGeometry(int isRed, RaycastHit hit)
    //{
    //    GameObject hitObj = hit.collider.gameObject;
    //    GameObject subCube = GameObject.CreatePrimitive(PrimitiveType.Cube);

    //    // Get Scale Values
    //    Vector3 scale = hit.transform.rotation * hit.transform.localScale;

    //    Vector3 normal = hit.normal;
    //    // Special Case if hit object is parallel to ground
    //    Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
    //    if (right == Vector3.zero)
    //    {
    //        right = _mainCam.right;
    //        subCube.transform.rotation = Quaternion.Euler(0f, _mainCam.rotation.eulerAngles.y, 0f);
    //    }
    //    subCube.transform.rotation *= hit.transform.rotation;
    //    Vector3 up = Vector3.Cross(right, normal).normalized;

    //    // Scale direction vectors
    //    Vector3 rightScale = portalWidth * 2 * right;
    //    Vector3 upScale = portalHeight * 2 * up;
    //    Vector3 forwardScale = Vector3.Dot(scale, normal) * normal;
    //    Vector3 newScale = Quaternion.Inverse(subCube.transform.rotation) * (rightScale + upScale + forwardScale);
    //    newScale = new(Mathf.Abs(newScale.x), Mathf.Abs(newScale.y), Mathf.Abs(newScale.z));

    //    subCube.transform.localScale = newScale;

    //    subCube.transform.position = hit.point + forwardScale.magnitude / 2 * -normal;

    //    // Create new mesh from subtraction
    //    //PerformCSGFunc(CSGFunc.Subtract, hitObj, subCube);

    //    // Hide original object
    //    Destroy(subCube);
    //    Destroy(hitObj);
    //}

    void SpawnPortal(int isRed, RaycastHit hit)
    {
        GameObject portal = Instantiate(portalPrefab, hit.point - _mainCam.forward * OFFSET, Quaternion.identity);
        Mesh mesh = portal.GetComponent<MeshFilter>().mesh;
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

        // Delete previous portal and reset surface layer
        Portal previousPortal = _portals[isRed];
        if (previousPortal != null)
        {
            _portalSurfaces[isRed].layer = _portalSurfaceLayers[isRed];
            Destroy(previousPortal.gameObject);
        }

        // Temporarily change layer for collision
        GameObject hitObject = hit.collider.gameObject;
        _portalSurfaces[isRed] = hitObject;
        _portalSurfaceLayers[isRed] = hitObject.layer;
        hitObject.layer = LayerMask.NameToLayer("PortalSurface");

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
    }

    /// <summary>
    /// Perform a CSG function based on its index
    /// </summary>
    /// <param name="funcIdx"></param>
    /// <param name="lhs">object to perform function on</param>
    /// <param name="rhs">object to subtract/add/intersect</param>
    /// <returns>Result GameObject</returns>
    // GameObject PerformCSGFunc(CSGFunc func, GameObject lhs, GameObject rhs)
    // {
    //     Model result;
    //     switch (func)
    //     {
    //         case CSGFunc.Subtract:
    //             result = CSG.Subtract(lhs, rhs);
    //             break;
    //         case CSGFunc.Union:
    //             result = CSG.Union(lhs, rhs);
    //             break;
    //         case CSGFunc.Intersect:
    //             result = CSG.Intersect(lhs, rhs);
    //             break;
    //         default:
    //             Debug.LogWarning(string.Format("CSG Function {0} not found", func));
    //             return null;
    //     }

    //     GameObject resultObj = new(string.Format("{0} Result", func));
    //     resultObj.AddComponent<MeshFilter>().sharedMesh = result.mesh;
    //     resultObj.AddComponent<MeshRenderer>().sharedMaterials = result.materials.ToArray();
    //     resultObj.AddComponent<MeshCollider>();
    //     resultObj.layer = lhs.layer;

    //     return resultObj;
    // }
}
