using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    public PortalColor Color { get => _color; set {
            _color = value;
            int layer;
            switch (_color)
            {
                case PortalColor.Red:
                    layer = LayerMask.NameToLayer("PortalRed");
                    gameObject.layer = layer;
                    portalCam.cullingMask |= 1 << layer;
                    break;
                case PortalColor.Blue:
                    layer = LayerMask.NameToLayer("PortalBlue");
                    gameObject.layer = layer;
                    portalCam.cullingMask |= 1 << layer;
                    break;
            }
        } }
    PortalColor _color = PortalColor.Red;
    Portal _linkedPortal;
    public Portal LinkedPortal { get => _linkedPortal; }
    [HideInInspector] public MeshRenderer screen;

    Camera _playerCam;
    [HideInInspector] public Camera portalCam;
    RenderTexture _tex;
    readonly HashSet<PortalEntity> entitiesInPortal = new();

    private void Awake()
    {
        _playerCam = Camera.main;
        portalCam = GetComponentInChildren<Camera>();
        portalCam.enabled = false;
        screen = GetComponent<MeshRenderer>();
    }

    private void LateUpdate()
    {
        if (IsLinked() && !VisibleInCamera(_linkedPortal.screen, _playerCam) && !VisibleInCamera(screen, _playerCam)) return;
        Render();
    }

    // CREDIT: Michael Garforth and Sebastian Lague
    bool VisibleInCamera(Renderer renderer, Camera camera)
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds);
    }

    void MoveAndRotateCamera()
    {
        // Flip linkedPortal so that we get the position behind the portal
        _linkedPortal.transform.rotation = Quaternion.LookRotation(-_linkedPortal.transform.forward, _linkedPortal.transform.up);

        // Convert player-to-portal distance into linkedPortal-to-player distance
        Matrix4x4 m = _linkedPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix * _playerCam.transform.localToWorldMatrix;

        // Unflip linked portal
        _linkedPortal.transform.rotation = Quaternion.LookRotation(-_linkedPortal.transform.forward, _linkedPortal.transform.up);

        portalCam.transform.SetPositionAndRotation(m.GetPosition(), m.rotation);
    }

    // I'M HIM!! (NO IM NOT)
    //void MoveAndRotateCamera()
    //{
    //    // Move Camera to match player's relative position
    //    Vector3 toPlayer = _playerCam.transform.position - transform.position;
    //    Vector3 relForward = Vector3.Dot(toPlayer, normal) * -_linkedPortal.normal; // Camera is behind portal
    //    Vector3 relUp = Vector3.Dot(toPlayer, up) * _linkedPortal.up;
    //    Vector3 relRight = Vector3.Dot(toPlayer, right) * -_linkedPortal.right; // Camera is mirrored
    //    Vector3 relPos = relForward + relUp + relRight;

    //    portalCam.transform.position = _linkedPortal.transform.position + relPos;

    //    // Rotate Camera
    //    Vector3 camRelForward = Vector3.Dot(_playerCam.forward, normal) * -_linkedPortal.normal;
    //    Vector3 camRelUp = Vector3.Dot(_playerCam.forward, up) * _linkedPortal.up;
    //    Vector3 camRight = Vector3.Dot(_playerCam.forward, right) * -_linkedPortal.right;
    //    Vector3 camForward = camRelForward + camRelUp + camRight;
    //    float angle = Mathf.Deg2Rad * Vector3.Angle(normal, _linkedPortal.normal);
    //    Vector3 planeNormal = Vector3.Cross(up, _linkedPortal.up);
    //    Vector3 camUp;
    //    if (planeNormal == Vector3.zero) // Portals have same up direction
    //    {
    //        camUp = _playerCam.up;
    //    }
    //    else camUp = _playerCam.up.RotateBy(angle, planeNormal, false);
    //    Quaternion camRot = Quaternion.LookRotation(camForward, camUp);
    //    portalCam.transform.rotation = camRot;
    //}

    public void SetDirections(Vector3 normal, Vector3 up)
    {
        transform.rotation = Quaternion.LookRotation(normal, up);
    }

    /// <summary>
    /// Sets the camera's oblique projection matrix so we can see through everything
    /// before the portal
    /// CREDIT: JoePatrick and https://www.terathon.com/lengyel/Lengyel-Oblique.pdf
    /// </summary>
    public void SetObliqueProjectionMatrix()
    {
        if (!IsLinked()) return;
        Vector3 normal = _linkedPortal.transform.forward;
        float nq = -Vector3.Dot(normal, _linkedPortal.transform.position);

        // Define clip plane
        Vector4 clipPlaneWorldSpace = new(normal.x, normal.y, normal.z, nq);
        Vector4 clipPlane = Matrix4x4.Transpose(portalCam.cameraToWorldMatrix) * clipPlaneWorldSpace;

        portalCam.projectionMatrix = _playerCam.CalculateObliqueMatrix(clipPlane);
    }

    public void CreateViewTexture()
    {
        if (IsLinked() && (_tex == null || _tex.width != Screen.width || _tex.height != Screen.height))
        {
            _tex?.Release();
            _tex = new RenderTexture(Screen.width, Screen.height, 0);

            portalCam.targetTexture = _tex;

            screen.material.SetTexture("_MainTex", _tex);
        }
    }

    public void Link(Portal other)
    {
        _linkedPortal = other;
        if (!other.IsLinked()) other.Link(this);

        // Reset texture
        _tex = null;
    }
    public bool IsLinked() => _linkedPortal != null;

    public void Render()
    {
        if (!IsLinked() || portalCam == null) return;

        CreateViewTexture();

        MoveAndRotateCamera();

        SetObliqueProjectionMatrix();

        // Make sure this camera sees what player sees
        portalCam.fieldOfView = _playerCam.fieldOfView;

        portalCam.Render();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Update tracked entities
        PortalEntity entity = new(other.transform);
        if (!entitiesInPortal.Contains(entity)) entitiesInPortal.Add(entity);

        // Disable collision while in portal
        //other.excludeLayers |= 1 << LayerMask.NameToLayer("PortalSurface");
    }

    private void OnTriggerExit(Collider other)
    {
        // Update tracked entities
        PortalEntity entity = new(other.transform);
        if (entitiesInPortal.Contains(entity)) entitiesInPortal.Remove(entity);

        // Reenable collision
        //other.excludeLayers &= ~(1 << LayerMask.NameToLayer("PortalSurface"));
    }

    public override string ToString()
    {
        return Color + " Portal";
    }
}

public enum PortalColor
{
    Red, Blue
}