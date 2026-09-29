using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRBaseInteractable))]
public class LaptopTerminal : MonoBehaviour
{
    [Header("Lid Movement")]
    [SerializeField] private Transform lidPivot;
    [SerializeField] private Vector3 closedRotation = Vector3.zero;
    [SerializeField] private Vector3 openRotation = new Vector3(-100f, 0, 0); 
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Screen Visuals")]
    [Tooltip("The UI Canvas to reveal when turned on.")]
    [SerializeField] private GameObject keypadCanvas;
    [Tooltip("The CanvasGroup on the UI to fade it in/out.")]
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [Tooltip("The black quad that covers the screen when turned off.")]
    [SerializeField] private GameObject blackoutScreen;
    
    [Header("Physics & Input")]
    [Tooltip("The main physical collider of the laptop to ignore when plugging in.")]
    [SerializeField] private Collider laptopMainCollider;
    [SerializeField] private InputActionReference secondaryButtonAction;

    private XRBaseInteractable interactable;
    private bool isHovered = false;
    private bool isLidOpen = false;
    private bool isFlashDrivePluggedIn = false;

    private Coroutine hingeCoroutine;
    private Coroutine bootCoroutine; 
    private Coroutine fadeCoroutine;

    void Start()
    {
        interactable = GetComponent<XRBaseInteractable>();
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);

        // Ensure popup starts fully transparent
        if (popupCanvasGroup != null) popupCanvasGroup.alpha = 0f;
        
        TurnOffScreenInstant();
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
            ToggleLid();
        }
    }

    private void ToggleLid()
    {
        isLidOpen = !isLidOpen;
        Quaternion targetRotation = isLidOpen ? Quaternion.Euler(openRotation) : Quaternion.Euler(closedRotation);

        if (hingeCoroutine != null) StopCoroutine(hingeCoroutine);
        hingeCoroutine = StartCoroutine(RotateLid(targetRotation));

        // Only manage screen power if the flash drive is supplying the data/auth
        if (isFlashDrivePluggedIn)
        {
            if (isLidOpen)
            {
                TurnOnScreenSequence();
            }
            else
            {
                TurnOffScreenSequence();
            }
        } 
    }

    private IEnumerator RotateLid(Quaternion targetRotation)
    {
        while (Quaternion.Angle(lidPivot.localRotation, targetRotation) > 0.1f)
        {
            lidPivot.localRotation = Quaternion.Slerp(lidPivot.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
            yield return null;
        }
        lidPivot.localRotation = targetRotation;
    }

    // --- HOVER METHODS FOR PHYSICS FIX ---
    public void OnSocketHoverEnter(HoverEnterEventArgs args)
    {
        Collider usbCollider = args.interactableObject.transform.GetComponent<Collider>();
        if (usbCollider != null && laptopMainCollider != null)
        {
            Physics.IgnoreCollision(usbCollider, laptopMainCollider, true); 
        }
    }

    public void OnSocketHoverExit(HoverExitEventArgs args)
    {
        Collider usbCollider = args.interactableObject.transform.GetComponent<Collider>();
        if (usbCollider != null && laptopMainCollider != null)
        {
            Physics.IgnoreCollision(usbCollider, laptopMainCollider, false); 
        }
    }

    // --- USB PLUG IN/OUT SOCKET EVENTS ---
    public void TurnOnScreen(SelectEnterEventArgs args)
    {
        isFlashDrivePluggedIn = true;
        
        // Boot up immediately if the lid is already open when inserted
        if (isLidOpen)
        {
            TurnOnScreenSequence();
        }
    }

    public void TurnOffScreen(SelectExitEventArgs args)
    {
        isFlashDrivePluggedIn = false;
        TurnOffScreenSequence();
    }

    // --- INTERNAL SCREEN & FADE LOGIC ---
    private void TurnOnScreenSequence()
    {   
        if (bootCoroutine != null) StopCoroutine(bootCoroutine);
        bootCoroutine = StartCoroutine(BootSequence());
    }

    private IEnumerator BootSequence()
    {
        yield return new WaitForSeconds(0.5f); 
        
        if (blackoutScreen != null) blackoutScreen.SetActive(false); 
        
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadePopup(1f));
    }

    private void TurnOffScreenSequence()
    {
        if (bootCoroutine != null) StopCoroutine(bootCoroutine);
        
        if (blackoutScreen != null) blackoutScreen.SetActive(true); 
        
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadePopup(0f));
    }

    private void TurnOffScreenInstant()
    {
        if (blackoutScreen != null) blackoutScreen.SetActive(true); 
        if (keypadCanvas != null) keypadCanvas.SetActive(false); 
    }

    private IEnumerator FadePopup(float targetAlpha)
    {
        if (popupCanvasGroup == null) yield break;

        // If fading in, activate the Canvas GameObject first so rendering begins
        if (targetAlpha > 0 && keypadCanvas != null) keypadCanvas.SetActive(true);

        float startAlpha = popupCanvasGroup.alpha;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            popupCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        
        popupCanvasGroup.alpha = targetAlpha;
        
        // Prevent invisible UI from blocking raycasts
        popupCanvasGroup.blocksRaycasts = (targetAlpha > 0f);
        popupCanvasGroup.interactable = (targetAlpha > 0f);

        // If fading out, turn off the Canvas GameObject entirely once invisible
        if (targetAlpha == 0 && keypadCanvas != null) keypadCanvas.SetActive(false);
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