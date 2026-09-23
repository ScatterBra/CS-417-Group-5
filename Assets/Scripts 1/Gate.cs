using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// Gate Affordance: grab/select the gate to teleport the player to a new spot (or load a new scene).
/// Hook a Lock's "On Unlocked" to Gate.Unlock() to make the gate a Reveal.
[RequireComponent(typeof(XRSimpleInteractable))]
public class Gate : MonoBehaviour
{
    public Transform destination;              // empty object where the player should appear
    public string destinationScene = "";       // if set, loads this scene instead
    public Transform xrOrigin;                 // leave empty to use the camera's root
    public bool startsUnlocked = true;
    public UnityEvent onTeleport;

    bool open;

    void Awake()
    {
        open = startsUnlocked;
        GetComponent<XRSimpleInteractable>().selectEntered.AddListener(_ => Enter());
    }

    public void Unlock() { open = true; }

    void Enter()
    {
        if (!open) return;
        onTeleport.Invoke();

        if (!string.IsNullOrEmpty(destinationScene))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(destinationScene);
            return;
        }
        if (!destination) return;

        Camera cam = Camera.main;
        if (!xrOrigin && cam) xrOrigin = cam.transform.root;
        if (!xrOrigin || !cam) return;

        // Match facing direction
        float yaw = destination.eulerAngles.y - cam.transform.eulerAngles.y;
        xrOrigin.RotateAround(cam.transform.position, Vector3.up, yaw);

        // Put the player's head (not the rig origin) on the destination
        Vector3 offset = cam.transform.position - xrOrigin.position;
        offset.y = 0f;
        xrOrigin.position = new Vector3(destination.position.x - offset.x,
                                        destination.position.y,
                                        destination.position.z - offset.z);
    }
}
