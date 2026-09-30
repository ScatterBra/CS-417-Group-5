using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRBaseInteractable))]
public class PropHighlighter : MonoBehaviour
{
    public enum PromptType { Grab, Interact, Custom }

    [Header("Hover Prompt Settings")]
    [Tooltip("Check this to automatically spawn a floating text prompt when hovered.")]
    [SerializeField] private bool showHoverPrompt = false;
    [SerializeField] private PromptType itemType = PromptType.Interact;
    [SerializeField] private string customPromptText;
    [SerializeField] private Vector3 promptOffset = new Vector3(0, 0.25f, 0);

    [Header("Highlight Settings")]
    [SerializeField] private MeshRenderer meshRenderer;
    [Tooltip("Leave blank to automatically load GrabHighlight or InteractHighlight from the Resources folder.")]
    [SerializeField] private Material customMaterialOverride;

    [Header("Distance Settings")]
    [SerializeField] private float maxHighlightDistance = 10.0f;
    [SerializeField] private float minHighlightDistance = 0.1f;
    
    [Header("Filter Strength")]
    [SerializeField] private float minFilterStrength = 0.03f;
    [SerializeField] private float maxFilterStrength = 0.33f;

    private Material instanceHighlightMaterial;
    private XRBaseInteractable interactable;
    private IXRHoverInteractor currentInteractor;
    private bool isHighlighted = false;

    private GameObject highlightOverlay;
    private MeshRenderer overlayRenderer;
    private GameObject promptRoot; 

    void Start()
    {
        // Determine which material template to use
        Material selectedTemplate = customMaterialOverride;

        // Auto-assign based on the dropdown if the override slot is left blank
        if (selectedTemplate == null)
        {
            if (itemType == PromptType.Grab)
            {
                selectedTemplate = Resources.Load<Material>("GrabHighlight");
            }
            else 
            {
                // Default to InteractHighlight for both Interact and Custom types
                selectedTemplate = Resources.Load<Material>("InteractHighlight");
            }

            if (selectedTemplate == null)
            {
                Debug.LogWarning("PropHighlighter: Could not find 'GrabHighlight' or 'InteractHighlight' in a Resources folder!");
            }
        }

        // Initialize the shell with the chosen material
        if (meshRenderer != null && selectedTemplate != null)
        {
            instanceHighlightMaterial = new Material(selectedTemplate);
            CreateOverlayShell();
        }

        if (showHoverPrompt)
        {
            GeneratePromptUI();
        }

        interactable = GetComponent<XRBaseInteractable>();
        interactable.hoverEntered.AddListener(EnableHighlight);
        interactable.hoverExited.AddListener(DisableHighlight);
    }

    private void CreateOverlayShell()
    {
        highlightOverlay = new GameObject("HighlightOverlay");
        highlightOverlay.transform.SetParent(meshRenderer.transform, false);
        
        MeshFilter originalFilter = meshRenderer.GetComponent<MeshFilter>();
        if (originalFilter != null)
        {
            MeshFilter newFilter = highlightOverlay.AddComponent<MeshFilter>();
            newFilter.sharedMesh = originalFilter.sharedMesh;
        }

        overlayRenderer = highlightOverlay.AddComponent<MeshRenderer>();
        Material[] overlayMats = new Material[meshRenderer.sharedMaterials.Length];
        for (int i = 0; i < overlayMats.Length; i++)
        {
            overlayMats[i] = instanceHighlightMaterial;
        }
        overlayRenderer.materials = overlayMats;

        highlightOverlay.SetActive(false);
    }

    private void GeneratePromptUI()
    {
        promptRoot = new GameObject("HoverPromptUI_" + gameObject.name, typeof(Canvas));
        
        // Leaving UI at the root level prevents it 
        // from inheriting the weird squished scale of objects like the keypad
        promptRoot.transform.SetParent(null); 

        Canvas canvas = promptRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform rect = promptRoot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 100);
        
        // Use a fixed, uniform scale since it's no longer a child
        rect.localScale = new Vector3(0.0015f, 0.0015f, 0.0015f);

        GameObject textObj = new GameObject("Text", typeof(Text));
        textObj.transform.SetParent(rect, false);
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;

        Text uiText = textObj.GetComponent<Text>();
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = 32;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.raycastTarget = false; 

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);

        if (itemType == PromptType.Grab) uiText.text = "Hold Grip to Grab";
        else if (itemType == PromptType.Interact) uiText.text = "Press Secondary to Interact";
        else uiText.text = customPromptText;

        promptRoot.SetActive(false);
    }
    
    void Update()
    {
        if (currentInteractor != null && isHighlighted && instanceHighlightMaterial != null)
        {
            float distance = Vector3.Distance(currentInteractor.transform.position, transform.position);
            float intensity = Mathf.InverseLerp(maxHighlightDistance, minHighlightDistance, distance);
            float currentStrength = Mathf.Lerp(minFilterStrength, maxFilterStrength, intensity);
            
            instanceHighlightMaterial.SetFloat("_FilterStrength", currentStrength);
        }
    }

    void LateUpdate()
    {
        if (showHoverPrompt && isHighlighted && promptRoot != null && Camera.main != null)
        {
            if (meshRenderer != null)
            {
                // Find the absolute top of the 3D model's geometry
                Vector3 topPosition = meshRenderer.bounds.center;
                topPosition.y = meshRenderer.bounds.max.y;

                // Move the UI to the top of the object + your custom offset
                promptRoot.transform.position = topPosition + promptOffset;
            }

            // Face the camera
            promptRoot.transform.rotation = Camera.main.transform.rotation;
        }
    }

    public void EnableHighlight(HoverEnterEventArgs args)
    {
        currentInteractor = args.interactorObject;
        isHighlighted = true;
        if (highlightOverlay != null) highlightOverlay.SetActive(true);
        if (showHoverPrompt && promptRoot != null) promptRoot.SetActive(true);
    }

    public void DisableHighlight(HoverExitEventArgs args)
    {
        currentInteractor = null;
        isHighlighted = false;
        if (highlightOverlay != null) highlightOverlay.SetActive(false);
        if (showHoverPrompt && promptRoot != null) promptRoot.SetActive(false);
    }

    public void SwapHighlightTemplate(Material newTemplate)
    {
        if (newTemplate == null) return;
        
        if (instanceHighlightMaterial != null) Destroy(instanceHighlightMaterial);
        instanceHighlightMaterial = new Material(newTemplate);
        
        if (overlayRenderer != null)
        {
            Material[] overlayMats = new Material[overlayRenderer.sharedMaterials.Length];
            for (int i = 0; i < overlayMats.Length; i++) overlayMats[i] = instanceHighlightMaterial;
            overlayRenderer.materials = overlayMats;
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
        
        // Because the UI is no longer a child, we must manually destroy it 
        // if the puzzle item gets destroyed or deleted.
        if (promptRoot) Destroy(promptRoot);
    }
}