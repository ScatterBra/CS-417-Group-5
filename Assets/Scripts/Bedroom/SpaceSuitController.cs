using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class SpaceSuitController : MonoBehaviour
{
    [Header("Equip Effects")]
    [SerializeField] private AudioClip equipSound;

    [Header("Input")]
    [SerializeField] private InputActionReference secondaryButtonAction;

    private XRSimpleInteractable interactable;
    private bool isHovered = false;
    private bool isEquipped = false;

    [Header("Celebration Sequence")]
    [Tooltip("Drag the external AudioSource here")]
    [SerializeField] private AudioSource celebrationAudioSource; 
    [SerializeField] private AudioClip celebrationSound;
    [Tooltip("Drag the disabled parent GameObject containing your guiding lights here.")]
    [SerializeField] private GameObject guidingLights; 
    [SerializeField] private float celebrationDelay = 1.0f;

    void Start()
    {
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
        if (!isEquipped && isHovered && secondaryButtonAction != null && secondaryButtonAction.action.WasPressedThisFrame())
        {
            EquipSuit();
        }
    }

    private void EquipSuit()
    {
        if (isEquipped) return; 
        isEquipped = true;

        StartCoroutine(EquipSequenceRoutine());
    }

    private System.Collections.IEnumerator EquipSequenceRoutine()
    {
        // Log team progress immediately
        GameProgress.TryCollectItem("Boss_SpaceSuit"); 
        GameProgress.CompleteRoom("BossBedroom"); 

        // Play the equip sound
        if (equipSound != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(equipSound, Camera.main.transform.position);
        }

        // Hide the physical suit in the room so the player thinks it is gone.
        // Disable renderers and colliders instead of SetActive(false) so this Coroutine survives
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

        yield return new WaitForSeconds(celebrationDelay);

        // Play the celebration sound
        if (celebrationAudioSource != null && celebrationSound != null)
        {
            celebrationAudioSource.PlayOneShot(celebrationSound);
        }

        // Turn on the guiding lights
        if (guidingLights != null)
        {
            guidingLights.SetActive(true);
        }

        gameObject.SetActive(false);
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