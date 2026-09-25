using UnityEngine;

/// Put on the Handmirror prop's PARENT (the grabbable handle object, with GrabbableProp on it).
/// Expects a child structure:
///   Handmirror (this script + GrabbableProp + Rigidbody + Collider + XR Grab Interactable)
///     MirrorCamera (Camera, Target Texture = a Render Texture you made)
///     MirrorDisplay (a Quad facing the player, material's texture = that same Render Texture)
/// This script flips MirrorDisplay so the rendered image reads left-right reversed,
/// which is what makes backwards writing in the room become readable through the mirror.
public class HandMirror : MonoBehaviour
{
    public Camera mirrorCamera;
    public Transform mirrorDisplay;      // the Quad showing the Render Texture
    public bool onlyRenderWhileHeld = true;  // saves performance when not in hand

    void Awake()
    {
        if (mirrorDisplay)
        {
            Vector3 s = mirrorDisplay.localScale;
            mirrorDisplay.localScale = new Vector3(-Mathf.Abs(s.x), s.y, s.z);
        }
        if (mirrorCamera && onlyRenderWhileHeld) mirrorCamera.enabled = false;

        var grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null && onlyRenderWhileHeld)
        {
            grab.selectEntered.AddListener(_ => { if (mirrorCamera) mirrorCamera.enabled = true; });
            grab.selectExited.AddListener(_ => { if (mirrorCamera) mirrorCamera.enabled = false; });
        }
    }
}
