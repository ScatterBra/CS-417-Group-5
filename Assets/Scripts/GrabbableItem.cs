using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Trigger grabbing through PlayerRig. Configure physics components separately.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ItemPrompt))]
public sealed class GrabbableItem : MonoBehaviour
{
    public const int GrabLayer = 1 << 8;

    private void Reset() => ConfigureGrab(false);
    private void Awake()
    {
        ConfigureGrab(true);
        if (GetComponent<ItemPrompt>() == null) gameObject.AddComponent<ItemPrompt>();
    }

    private void ConfigureGrab(bool reportMissing)
    {
        // Check first: adding XRGrabInteractable must not auto-create a Rigidbody.
        if (GetComponent<Rigidbody>() == null || GetComponentInChildren<Collider>() == null)
        {
            if (reportMissing)
                Debug.LogWarning("GrabbableItem needs a manually configured Rigidbody and Collider.", this);
            return;
        }

        XRGrabInteractable grab = GetComponent<XRGrabInteractable>();
        if (grab == null) grab = gameObject.AddComponent<XRGrabInteractable>();
        grab.interactionLayers = GrabLayer;
        grab.useDynamicAttach = true;
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grab.throwOnDetach = true;
    }
}
