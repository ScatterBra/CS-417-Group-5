using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class PropHighlighter : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material highlightMaterialTemplate;

    [Header("Distance Settings")]
    [SerializeField] private float maxHighlightDistance = 2f;
    [SerializeField] private float maxFilterStrength = 0.4f;
    [SerializeField] private float maxOutlineThickness = 4f;

    private Material originalMaterial;
    private Material instanceHighlightMaterial;
    private XRGrabInteractable interactable;
    private IXRHoverInteractor currentInteractor;

    void Start()
    {
        if (meshRenderer)
        {
            originalMaterial = meshRenderer.material;

            instanceHighlightMaterial = new Material(highlightMaterialTemplate);

            if (originalMaterial.HasProperty("_MainTex") || originalMaterial.HasProperty("_BaseMap"))
            {
                Texture originalTex = originalMaterial.mainTexture;
                instanceHighlightMaterial.SetTexture("_MainText", originalTex);
            }
        }

        // Set up hover events 
        interactable = GetComponent<XRGrabInteractable>();
        interactable.hoverEntered.AddListener(EnableHighlight);
        interactable.hoverExited.AddListener(DisableHighlight);
    }

    void Update()
    {
        if (currentInteractor != null && meshRenderer.material == instanceHighlightMaterial)
        {
            float distance = Vector3.Distance(currentInteractor.transform.position, transform.position);

            // Closer distance is more intensity
            float intensity = Mathf.Clamp01(1f - (distance - maxHighlightDistance));

            instanceHighlightMaterial.SetFloat("_FilterStrength", intensity * maxFilterStrength);
            instanceHighlightMaterial.SetFloat("_OutlineThickness", intensity * maxOutlineThickness);
        }
    }


    public void EnableHighlight(HoverEnterEventArgs args)
    {
        if (meshRenderer)
        {
            currentInteractor = args.interactorObject;
            meshRenderer.material = instanceHighlightMaterial;
        }
    }

    public void DisableHighlight(HoverExitEventArgs args)
    {
        if (meshRenderer)
        {
            currentInteractor = null;
            meshRenderer.material = originalMaterial;
        }
    }

    private void OnDestroy()
    {
        if (interactable)
        {
            interactable.hoverEntered.RemoveListener(EnableHighlight);
            interactable.hoverExited.RemoveListener(DisableHighlight);
        }
        if (instanceHighlightMaterial)
        {
            Destroy(instanceHighlightMaterial);
        }
    }
}
