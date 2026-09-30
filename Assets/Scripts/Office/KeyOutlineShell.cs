using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws the team's Group5/KeyOutline shell around this object.
/// The shader works by drawing a second, slightly inflated copy of the mesh,
/// so this builds that copy as a child at runtime - the same "KeyOutline"
/// child the lab scene sets up by hand, without anyone having to set it up.
/// </summary>
[DisallowMultipleComponent]
public sealed class KeyOutlineShell : MonoBehaviour
{
    [Tooltip("A material that uses the Group5/KeyOutline shader.")]
    public Material outlineMaterial;

    [Tooltip("Keep the material's Base Map as an alpha mask. Only turn this on when the " +
             "texture was painted for this mesh's UVs; a mask from another model cuts random holes.")]
    public bool keepAlphaMask;

    private MeshRenderer shell;
    private Material shellMaterial;

    /// <summary>True while the outline is being drawn.</summary>
    public bool isVisible => shell != null && shell.enabled;

    private void Awake()
    {
        MeshFilter source = GetComponent<MeshFilter>();
        if (source == null)
        {
            source = GetComponentInChildren<MeshFilter>();
        }

        if (source == null || source.sharedMesh == null || outlineMaterial == null)
        {
            Debug.LogWarning("KeyOutlineShell needs a mesh and an outline material.", this);
            return;
        }

        shellMaterial = new Material(outlineMaterial);
        if (!keepAlphaMask)
        {
            shellMaterial.SetTexture("_BaseMap", Texture2D.whiteTexture);
        }

        GameObject child = new GameObject("KeyOutline");
        child.transform.SetParent(source.transform, false);
        child.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;

        shell = child.AddComponent<MeshRenderer>();
        Material[] materials = new Material[source.sharedMesh.subMeshCount];
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = shellMaterial;
        }

        shell.sharedMaterials = materials;
        shell.shadowCastingMode = ShadowCastingMode.Off;
        shell.receiveShadows = false;
        shell.lightProbeUsage = LightProbeUsage.Off;
        shell.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    /// <summary>Show or hide the outline.</summary>
    public void SetVisible(bool visible)
    {
        if (shell != null)
        {
            shell.enabled = visible;
        }
    }

    private void OnDestroy()
    {
        if (shellMaterial != null)
        {
            Destroy(shellMaterial);
        }
    }
}
