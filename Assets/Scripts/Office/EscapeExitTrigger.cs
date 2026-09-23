using UnityEngine;

/// <summary>
/// Sits just outside the door. When the player walks through an open door the
/// run is over, which is the beat the walkthrough video ends on.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class EscapeExitTrigger : MonoBehaviour
{
    public EscapeController escapeController;

    [Tooltip("Root of the XR rig. Anything parented under it counts as the player.")]
    public Transform playerRoot;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (escapeController == null || !IsPlayer(other))
        {
            return;
        }

        escapeController.ReportEscaped();
    }

    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            return true;
        }

        return playerRoot != null && other.transform.IsChildOf(playerRoot);
    }
}
