using UnityEngine;


/// Put on the Magnifying Glass prop's PARENT (the grabbable handle, with GrabbableProp on it).
/// Expects a child structure:
///   MagnifyingGlass (this script + GrabbableProp + Rigidbody + Collider + XR Grab Interactable)
///     LensCamera (Camera, Target Texture = a Render Texture, low Field of View = zoomed in)
///     LensDisplay (a Quad/Circle facing the player, material's texture = that same Render Texture)
/// LensCamera's forward direction should point the same way as the lens glass faces (away from the handle),
/// so it captures whatever the player holds the glass up to.
public class MagnifyingGlass : MonoBehaviour
{
    public Camera lensCamera;
    [Tooltip("Lower = more zoomed in. A normal camera is ~60. Try 15-25 for a strong magnify effect.")]
    [Range(1f, 60f)] public float zoomFOV = 20f;
    public bool onlyRenderWhileHeld = true;

    void Awake()
    {
        if (lensCamera) lensCamera.fieldOfView = zoomFOV;
        if (lensCamera && onlyRenderWhileHeld) lensCamera.enabled = false;

        var grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null && onlyRenderWhileHeld)
        {
            grab.selectEntered.AddListener(_ => { if (lensCamera) lensCamera.enabled = true; });
            grab.selectExited.AddListener(_ => { if (lensCamera) lensCamera.enabled = false; });
        }
    }
}
