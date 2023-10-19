using UnityEngine;
using ExtensionMethods;

public class Portal : MonoBehaviour
{
    public PortalColor color = PortalColor.Red;
    public Vector3 normal = Vector3.forward;
    public Vector3 up = Vector3.up;
    public Vector3 right = Vector3.right;
    Portal _linkedPortal;
    public Portal LinkedPortal { get => _linkedPortal; }

    Transform _playerCam;
    [HideInInspector] public Camera portalCam;
    RenderTexture _tex;
    MeshRenderer _meshRenderer;

    private void Awake()
    {
        _playerCam = Camera.main.transform;
        portalCam = GetComponentInChildren<Camera>();
        portalCam.enabled = false;
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void LateUpdate()
    {
        Render();
    }

    // I'M HIM!!
    void MoveAndRotateCamera()
    {
        // Move Camera to match player's relative position
        Vector3 toPlayer = _playerCam.transform.position - transform.position;
        Vector3 relForward = Vector3.Dot(toPlayer, normal) * -_linkedPortal.normal; // Camera is behind portal
        Vector3 relUp = Vector3.Dot(toPlayer, up) * _linkedPortal.up;
        Vector3 relRight = Vector3.Dot(toPlayer, right) * -_linkedPortal.right; // Camera is mirrored
        Vector3 relPos = relForward + relUp + relRight;

        portalCam.transform.position = _linkedPortal.transform.position + relPos;

        // BAD CODE BELOW
        /*Vector3 toPlayer = _playerCam.transform.position - transform.position;
        Vector3 toOtherPortal = _linkedPortal.transform.position - transform.position;
        Vector3 planeNormal = Vector3.Cross(toPlayer, toOtherPortal).normalized;

        Vector3 linkedNormal = -_linkedPortal.normal;
        float angle = Mathf.Deg2Rad * Vector3.Angle(normal, linkedNormal);
        Vector3 relativePos = toPlayer.RotateBy(angle, planeNormal, false);
        _portalCam.transform.position = _linkedPortal.transform.position + relativePos;*/

        // Rotate Camera
        Vector3 camRelForward = Vector3.Dot(_playerCam.forward, normal) * -_linkedPortal.normal;
        Vector3 camRelUp = Vector3.Dot(_playerCam.forward, up) * _linkedPortal.up;
        Vector3 camRight = Vector3.Dot(_playerCam.forward, right) * -_linkedPortal.right;
        Vector3 camForward = camRelForward + camRelUp + camRight;
        float angle = Mathf.Deg2Rad * Vector3.Angle(normal, _linkedPortal.normal);
        Vector3 planeNormal = Vector3.Cross(up, _linkedPortal.up);
        Vector3 camUp;
        if (planeNormal == Vector3.zero) // Portals have same up direction
        {
            camUp = _playerCam.up;
        }
        else camUp = _playerCam.up.RotateBy(angle, planeNormal, false);
        Quaternion camRot = Quaternion.LookRotation(camForward, camUp);
        portalCam.transform.rotation = camRot;
    }

    public void SetDirections(Vector3 normal, Vector3 up, Vector3 right)
    {
        this.normal = normal;
        this.up = up;
        this.right = right;
    }

    public void CreateViewTexture()
    {
        if (IsLinked() && (_tex == null || _tex.width != Screen.width || _tex.height != Screen.height))
        {
            if (_tex != null) _tex.Release();
            _tex = new RenderTexture(Screen.width, Screen.height, 0);

            portalCam.targetTexture = _tex;

            _meshRenderer.material.SetTexture("_MainTex", _tex);
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

        portalCam.Render();
    }

    private void OnTriggerEnter(Collider other)
    {
        
    }

    public override string ToString()
    {
        return color + " Portal";
    }
}

public enum PortalColor
{
    Red, Blue
}