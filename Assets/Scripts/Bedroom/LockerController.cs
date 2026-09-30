using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class LockerController : MonoBehaviour
{
    [Header("UI Control")]
    [Tooltip("The Keypad UI Canvas GameObject to activate.")]
    [SerializeField] private GameObject keypadUI;

    [Header("Locker Door State")]
    public bool isLocked = true;
    private bool isOpen = false;
    private bool isHovered = false;
    private bool isKeypadHovered = false;

    [Header("Locked Feedback UI")]
    [Tooltip("Drag a disabled UI GameObject here to show when locked.")]
    [SerializeField] private GameObject lockedTextUI;
    [SerializeField] private float lockedTextDuration = 2.0f;
    private Coroutine lockedTextCoroutine;

    [Header("Door Hinge")]
    [Tooltip("The Transform that pivots. If this script is on the door itself, you can drag the door here.")]
    [SerializeField] private Transform doorPivot;
    [SerializeField] private Vector3 closedRotation = Vector3.zero;
    [SerializeField] private Vector3 openRotation = new Vector3(0, -90f, 0); 
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Door Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip unlockSound;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorCloseSound;

    [Header("Input")]
    [SerializeField] private InputActionReference secondaryButtonAction;

    private XRSimpleInteractable interactable;
    private Coroutine hingeCoroutine;

    void Start()
    {
        if (doorPivot == null) doorPivot = transform;

        interactable = GetComponent<XRSimpleInteractable>();
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);

        doorPivot.localRotation = Quaternion.Euler(closedRotation);

        if (keypadUI != null) keypadUI.SetActive(false); 
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
        // Try to open/close the door
        if (isHovered && secondaryButtonAction != null && secondaryButtonAction.action.WasPressedThisFrame())
        {
            ToggleDoor();
        }

        // Try to open the Keypad UI
        if (isKeypadHovered && isLocked && secondaryButtonAction != null && secondaryButtonAction.action.WasPressedThisFrame())
        {
            ToggleKeypadUI();
        }
    }

    private void ToggleDoor()
    {
        // If it's locked and currently closed, show that its locked
        if (isLocked && !isOpen)
        {
            if (lockedTextCoroutine != null) StopCoroutine(lockedTextCoroutine);
            lockedTextCoroutine = StartCoroutine(ShowLockedText());
            return;
        }

        // Force the UI to close when the door moves 
        if (keypadUI != null) 
        {
            keypadUI.SetActive(false);
        }
        // ------------------------------------------------------

        isOpen = !isOpen;

        if (isOpen)
        {
            if (audioSource != null && doorOpenSound != null)
                audioSource.PlayOneShot(doorOpenSound);
            
            if (hingeCoroutine != null) StopCoroutine(hingeCoroutine);
            hingeCoroutine = StartCoroutine(RotateDoor(Quaternion.Euler(openRotation)));
        }
        else
        {
            if (audioSource != null && doorCloseSound != null)
                audioSource.PlayOneShot(doorCloseSound);

            if (hingeCoroutine != null) StopCoroutine(hingeCoroutine);
            hingeCoroutine = StartCoroutine(RotateDoor(Quaternion.Euler(closedRotation)));
        }
    }

    // --- CALLED BY THE PHYSICAL KEYPAD CHILD ---
    public void ToggleKeypadUI()
    {
        // Removed the 'isLocked' check so the player can open/close the UI 
        // even after it says "SOLVED"
        if (keypadUI != null)
        {
            keypadUI.SetActive(!keypadUI.activeSelf);
        }
    }

    // --- CALLED BY THE KEYPAD CONTROLLER'S SUCCESS EVENT ---
    public void UnlockLocker()
    {
        if (!isLocked) return;
        
        isLocked = false;

        // Play the click/unlock sound so the player knows it worked
        if (audioSource != null && unlockSound != null)
        {
            audioSource.PlayOneShot(unlockSound);
        }
    }

    private IEnumerator RotateDoor(Quaternion targetRotation)
    {
        if (doorPivot == null) yield break;

        while (Quaternion.Angle(doorPivot.localRotation, targetRotation) > 0.1f)
        {
            doorPivot.localRotation = Quaternion.Slerp(doorPivot.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
            yield return null;
        }
        doorPivot.localRotation = targetRotation;
    }

    private IEnumerator ShowLockedText()
    {
        if (lockedTextUI != null)
        {
            lockedTextUI.SetActive(true);
            yield return new WaitForSeconds(lockedTextDuration);
            lockedTextUI.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEnter);
            interactable.hoverExited.RemoveListener(OnHoverExit);
        }
    }

    public void OnKeypadHoverEnter(HoverEnterEventArgs args) => isKeypadHovered = true;
    public void OnKeypadHoverExit(HoverExitEventArgs args) => isKeypadHovered = false;
}