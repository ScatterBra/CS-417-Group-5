using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Applies a colored filter over the object material. The strength of the filter 
/// scales with distance up to a max strength. The filter properties are determined
/// based on the highMaterialTemplate provided to this
/// </summary>
[RequireComponent(typeof(XRBaseInteractable))]
public class PropHighlighter : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material highlightMaterialTemplate;

    [Header("Distance Settings")]
        // Static variable synced from the ReelClamper
    [SerializeField] private float maxHighlightDistance = 10.0f;
    [SerializeField] private float minHighlightDistance = 0.1f;
    
    [Header("Filter Strength")]
    [SerializeField] private float minFilterStrength = 0.05f;
    [SerializeField] private float maxFilterStrength = 0.33f;

    private Material[] originalMaterials;
    private Material instanceHighlightMaterial;
    private XRBaseInteractable interactable;
    private IXRHoverInteractor currentInteractor;
    private bool isHighlighted = false;

    void Start()
    {
        if (meshRenderer)
        {
            originalMaterials = meshRenderer.materials;
            instanceHighlightMaterial = new Material(highlightMaterialTemplate);
        }

        interactable = GetComponent<XRBaseInteractable>();
        interactable.hoverEntered.AddListener(EnableHighlight);
        interactable.hoverExited.AddListener(DisableHighlight);
    }

    void Update()
    {
        if (currentInteractor != null && isHighlighted)
        {
            float distance = Vector3.Distance(currentInteractor.transform.position, transform.position);
            
            // Calculate the 0 to 1 intensity based on the global max distance
            float intensity = Mathf.InverseLerp(maxHighlightDistance, minHighlightDistance, distance);
            
            // Blend between the minimum and maximum strength based on that intensity
            float currentStrength = Mathf.Lerp(minFilterStrength, maxFilterStrength, intensity);
            
            instanceHighlightMaterial.SetFloat("_FilterStrength", currentStrength);
        }
    }

    public void EnableHighlight(HoverEnterEventArgs args)
    {
        if (meshRenderer)
        {
            currentInteractor = args.interactorObject;
            isHighlighted = true;

            Material[] highlightMaterials = new Material[2];
            if (originalMaterials.Length > 0)
            {
                highlightMaterials[0] = originalMaterials[0];
            }
            highlightMaterials[1] = instanceHighlightMaterial;
            meshRenderer.materials = highlightMaterials;
        }
    }

    public void DisableHighlight(HoverExitEventArgs args)
    {
        if (meshRenderer)
        {
            currentInteractor = null;
            isHighlighted = false;
            meshRenderer.materials = originalMaterials;
        }
    }

    private void OnDestroy()
    {
        if (interactable)
        {
            interactable.hoverEntered.RemoveListener(EnableHighlight);
            interactable.hoverExited.RemoveListener(DisableHighlight);
        }
        if (instanceHighlightMaterial) Destroy(instanceHighlightMaterial);
    }
}