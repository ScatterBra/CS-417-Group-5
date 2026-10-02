using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRBaseInteractable))]
public class ConsoleController : MonoBehaviour
{
    [Header("TV Control")]
    public TVScreenFader tvFader; 

    [Header("Visual Feedback (Console Highlight)")]
    public PropHighlighter consoleHighlighter;
    public Material normalHighlightTemplate;
    public Material activeHighlightTemplate;

    [Header("Physics & Input")]
    public Collider consoleMainCollider;
    public InputActionReference secondaryButtonAction;

    [Header("Disk Drive Mechanics")]
    [Tooltip("The Attach Point assigned to the Socket (the object that actually moves).")]
    public Transform diskAttachPoint; 
    [Tooltip("Where the disk should sit deep inside the console.")]
    public Transform insideAttachPoint; 
    [Tooltip("Where the disk should pop out to (far enough to clear all colliders).")]
    public Transform ejectAttachPoint; 
    public float slideDuration = 0.5f;

    private XRBaseInteractable interactable; 
    private bool isConsoleHovered = false;
    private bool isDiskInserted = false;
    
    private Coroutine slideCoroutine;
    private Collider[] currentDiskColliders; 

    void Start()
    {
        interactable = GetComponent<XRBaseInteractable>();
        interactable.hoverEntered.AddListener(OnConsoleHoverEnter);
        interactable.hoverExited.AddListener(OnConsoleHoverExit);

        // Snap the socket to the eject point on startup so it's ready to receive the disk
        if (diskAttachPoint != null && ejectAttachPoint != null)
        {
            diskAttachPoint.localPosition = ejectAttachPoint.localPosition;
        }
    }

    void OnEnable()
    {
        if (secondaryButtonAction != null && secondaryButtonAction.action != null)
            secondaryButtonAction.action.Enable();
    }

    void OnDisable()
    {
        if (secondaryButtonAction != null && secondaryButtonAction.action != null)
            secondaryButtonAction.action.Disable();
    }

    private void OnConsoleHoverEnter(HoverEnterEventArgs args) => isConsoleHovered = true;
    private void OnConsoleHoverExit(HoverExitEventArgs args) => isConsoleHovered = false;

    void Update()
    {
        if (isConsoleHovered && isDiskInserted && secondaryButtonAction != null && secondaryButtonAction.action.WasPressedThisFrame())
        {
            EjectDisk();
        }
    }

    public void OnSocketHoverEnter(HoverEnterEventArgs args)
    {
        Collider[] diskColliders = args.interactableObject.transform.GetComponentsInChildren<Collider>();
        foreach (Collider col in diskColliders)
        {
            if (consoleMainCollider != null) Physics.IgnoreCollision(col, consoleMainCollider, true); 
        }
    }

    public void OnSocketHoverExit(HoverExitEventArgs args)
    {
        if (isDiskInserted) return;

        Collider[] diskColliders = args.interactableObject.transform.GetComponentsInChildren<Collider>();
        foreach (Collider col in diskColliders)
        {
            if (consoleMainCollider != null) Physics.IgnoreCollision(col, consoleMainCollider, false); 
        }
    }

    public void OnDiskInserted(SelectEnterEventArgs args)
    {
        isDiskInserted = true;
        
        currentDiskColliders = args.interactableObject.transform.GetComponentsInChildren<Collider>();
        foreach (Collider col in currentDiskColliders)
        {
            col.isTrigger = true;
        }
        
        if (tvFader != null) tvFader.FadeScreenIn();
        if (consoleHighlighter != null && activeHighlightTemplate != null)
            consoleHighlighter.SwapHighlightTemplate(activeHighlightTemplate);

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        if (insideAttachPoint != null) 
            slideCoroutine = StartCoroutine(SlideDisk(insideAttachPoint.localPosition, false));
    }

    public void OnDiskRemoved(SelectExitEventArgs args)
    {
        isDiskInserted = false; 

        if (currentDiskColliders != null)
        {
            foreach (Collider col in currentDiskColliders)
            {
                col.isTrigger = false; 
            }
            currentDiskColliders = null;
        }
        
        if (diskAttachPoint != null && ejectAttachPoint != null) 
            diskAttachPoint.localPosition = ejectAttachPoint.localPosition;
    }

    private void EjectDisk()
    {
        if (tvFader != null) tvFader.FadeScreenOut();
        if (consoleHighlighter != null && normalHighlightTemplate != null)
            consoleHighlighter.SwapHighlightTemplate(normalHighlightTemplate);

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        if (ejectAttachPoint != null) 
            slideCoroutine = StartCoroutine(SlideDisk(ejectAttachPoint.localPosition, true));
    }

    private IEnumerator SlideDisk(Vector3 targetPos, bool isEjecting)
    {
        if (diskAttachPoint == null) yield break;

        Vector3 startPos = diskAttachPoint.localPosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            diskAttachPoint.localPosition = Vector3.Lerp(startPos, targetPos, elapsed / slideDuration);
            yield return null;
        }
        diskAttachPoint.localPosition = targetPos;

        if (isEjecting && currentDiskColliders != null)
        {
            foreach (Collider col in currentDiskColliders)
            {
                col.isTrigger = false;
            }
        }
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnConsoleHoverEnter);
            interactable.hoverExited.RemoveListener(OnConsoleHoverExit);
        }
    }
}