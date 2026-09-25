using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class Drawer : MonoBehaviour
{
    [Header("Drawer State")]
    public bool requiresKey = false;
    public bool isLocked = false;
    private bool isOpen = false;
    private bool isHovered = false;

    [Header("Movement")]
    [SerializeField] private Transform drawerTransform;
    [SerializeField] private float openZOffset = 0.5f; 
    [SerializeField] private float lerpSpeed = 5f;

    [Header("Drawer Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip lockedRattleSound;
    [SerializeField] private AudioClip latchUnlockSound;
    [SerializeField] private AudioClip drawerOpenSound;
    [SerializeField] private AudioClip drawerCloseSound;

    [Header("Input")]
    [SerializeField] private InputActionReference secondaryButtonAction;

    private XRSimpleInteractable interactable;
    private Rigidbody rb;
    private Vector3 closedPosition;
    private Vector3 targetPosition;
    private Coroutine moveCoroutine;

    void Start()
    {
        if (drawerTransform == null) drawerTransform = transform;
        
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; 

        closedPosition = drawerTransform.localPosition;
        targetPosition = closedPosition;

        if (!requiresKey) isLocked = false;

        interactable = GetComponent<XRSimpleInteractable>();
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
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

    private void OnHoverEnter(HoverEnterEventArgs args) => isHovered = true;
    private void OnHoverExit(HoverExitEventArgs args) => isHovered = false;

    void Update()
    {
        if (isHovered && secondaryButtonAction != null && secondaryButtonAction.action.WasPressedThisFrame())
        {
            ToggleDrawer();
        }
    }

    private void ToggleDrawer()
    {
        // ONLY prevent movement if the drawer is locked AND currently closed.
        // If it's locked but open, allow the player to close it.
        if (isLocked && !isOpen)
        {
            if (audioSource && lockedRattleSound) audioSource.PlayOneShot(lockedRattleSound);
            return;
        }

        isOpen = !isOpen;

        if (isOpen)
        {
            targetPosition = closedPosition + new Vector3(0, 0, openZOffset);
            if (audioSource && drawerOpenSound) audioSource.PlayOneShot(drawerOpenSound);
        }
        else
        {
            targetPosition = closedPosition;
            if (audioSource && drawerCloseSound) audioSource.PlayOneShot(drawerCloseSound);
        }
        
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveDrawerPhysics());
    }

    public void Unlock()
    {
        if (!isLocked) return;
        isLocked = false;
        if (audioSource && latchUnlockSound) audioSource.PlayOneShot(latchUnlockSound);
    }

    // New method called by the scanner after 5 seconds
    public void Lock()
    {
        if (!requiresKey) return;
        isLocked = true;
    }

    private IEnumerator MoveDrawerPhysics()
    {
        while (Vector3.Distance(drawerTransform.localPosition, targetPosition) > 0.001f)
        {
            Vector3 nextLocalPos = Vector3.Lerp(drawerTransform.localPosition, targetPosition, Time.fixedDeltaTime * lerpSpeed);
            
            Vector3 nextWorldPos = drawerTransform.parent != null 
                ? drawerTransform.parent.TransformPoint(nextLocalPos) 
                : nextLocalPos;
            
            rb.MovePosition(nextWorldPos);
            yield return new WaitForFixedUpdate();
        }
        
        Vector3 finalWorldPos = drawerTransform.parent != null 
            ? drawerTransform.parent.TransformPoint(targetPosition) 
            : targetPosition;
            
        rb.MovePosition(finalWorldPos);
        drawerTransform.localPosition = targetPosition; 
    }
    
    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEnter);
            interactable.hoverExited.RemoveListener(OnHoverExit);
        }
    }
}