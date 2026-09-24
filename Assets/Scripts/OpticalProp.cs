using UnityEngine;

/// <summary>A reusable camera surface for a handheld mirror or magnifier.</summary>
public sealed class OpticalProp : MonoBehaviour
{
    public bool isMirror;
    [Min(1f)] public float magnification = 2f;
    public Camera viewCamera;
    public Renderer viewSurface;
    public Vector2 surfaceSize = new Vector2(0.2f, 0.24f);
    public RenderTexture textureTemplate;

    private RenderTexture liveTexture;
    private Material liveMaterial;
    private Camera playerCamera;

    private void Awake()
    {
        // Each prefab instance needs its own image; the scene objects already exist.
        liveTexture = new RenderTexture(textureTemplate);
        liveTexture.Create();
        liveMaterial = new Material(viewSurface.sharedMaterial);
        liveMaterial.SetTexture("_BaseMap", liveTexture);
        liveMaterial.SetFloat("_FlipX", isMirror ? 1f : 0f);
        viewSurface.sharedMaterial = liveMaterial;
        viewCamera.targetTexture = liveTexture;
    }

    private void LateUpdate()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null) { viewCamera.enabled = false; return; }
        Transform surface = viewSurface.transform;
        Vector3 normal = -surface.forward;
        Vector3 eye = playerCamera.transform.position;
        float distance = Vector3.Dot(eye - surface.position, normal);
        viewCamera.enabled = distance > 0.04f && Vector3.Distance(eye, surface.position) < 3f;
        if (!viewCamera.enabled) return;

        Vector3 position = isMirror ? eye - 2f * distance * normal : eye;
        viewCamera.transform.SetPositionAndRotation(position,
            Quaternion.LookRotation(isMirror ? normal : -normal, surface.up));
        Vector3 center = viewCamera.transform.InverseTransformPoint(surface.position);
        float zoom = isMirror ? 1f : magnification;
        float halfWidth = surfaceSize.x / (2f * zoom);
        float halfHeight = surfaceSize.y / (2f * zoom);
        // Start at the optical surface so the frame and objects behind it are clipped.
        float near = Mathf.Max(0.01f, center.z);
        viewCamera.nearClipPlane = near;
        viewCamera.projectionMatrix = Matrix4x4.Frustum(center.x - halfWidth,
            center.x + halfWidth, center.y - halfHeight, center.y + halfHeight, near, 100f);
    }

    private void OnDisable() { if (viewCamera != null) viewCamera.enabled = false; }

    private void OnDestroy()
    {
        if (viewCamera != null) viewCamera.targetTexture = null;
        if (liveTexture != null) { liveTexture.Release(); Destroy(liveTexture); }
        if (liveMaterial != null) Destroy(liveMaterial);
    }
}
