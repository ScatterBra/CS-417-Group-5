using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// Goes on an object that has an XR Socket Interactor.
/// Accepts only the Key Prop whose keyId matches requiredKeyId, then plays eased state changes.
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor))]
public class Lock : MonoBehaviour
{
    [Header("Which key fits")]
    public string requiredKeyId = "";          // empty = any key fits

    [Header("Escape progress")]
    [Tooltip("Tick for the locks that must be solved to escape. Untick for side locks (e.g. a crate).")]
    public bool countsTowardEscape = true;

    [Header("Reuse")]
    [Tooltip("Tick for a lock that should fire again each time a new key is inserted (e.g. a battery slot). " +
             "Leave off for one-time escape locks. Also untick Lock Key In Place below so the key can be pulled back out.")]
    public bool reusable = false;

    [Header("When unlocked")]
    public bool lockKeyInPlace = true;         // key stays seated and can't be pulled back out
    public EasedStateChange[] stateChanges;    // door slides, bulb lights up, etc.
    public UnityEvent onUnlocked;              // e.g. Gate.Unlock(), reveal an object
    public UnityEvent onWrongKey;              // e.g. play a "nope" sound

    public bool IsUnlocked { get; private set; }
    bool reportedToManager;

    UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;

    void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        socket.recycleDelayTime = 1.5f;        // stops a rejected key from instantly re-snapping
        socket.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDestroy()
    {
        if (socket) socket.selectEntered.RemoveListener(OnSelectEntered);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (IsUnlocked && !reusable) return;

        Transform key = args.interactableObject.transform;
        var prop = key.GetComponent<GrabbableProp>();
        bool correct = prop != null && prop.isKey &&
                       (string.IsNullOrEmpty(requiredKeyId) || prop.keyId == requiredKeyId);

        if (!correct)
        {
            Debug.Log($"[Lock] {name}: rejected '{key.name}' " +
                      (prop == null ? "(no GrabbableProp on it)" :
                       !prop.isKey ? "(Is Key is off)" :
                       $"(its Key Id is '{prop.keyId}', this lock needs '{requiredKeyId}')"), this);
            onWrongKey.Invoke();
            StartCoroutine(Reject(args.interactableObject));
            return;
        }

        Unlock(lockKeyInPlace ? key : null);
    }

    void Unlock(Transform keyToSeat)
    {
        if (IsUnlocked && !reusable) return;
        IsUnlocked = true;
        Debug.Log($"[Lock] {name}: UNLOCKED{(reusable ? " (reusable)" : "")}", this);
        foreach (var s in stateChanges) if (s) s.Play();   // one-shot effects only really play the first time
        onUnlocked.Invoke();
        if (countsTowardEscape && !reportedToManager && GameManager.Instance)
        {
            GameManager.Instance.LockSolved(this);
            reportedToManager = true;
        }
        if (keyToSeat) StartCoroutine(Seat(keyToSeat));
    }

    /// Right-click the Lock component title > "Test: Unlock Now" while in Play mode.
    /// Runs everything a real key would (move door, events, scoreboard) without needing a headset.
    [ContextMenu("Test: Unlock Now")]
    void TestUnlock()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Enter Play mode first."); return; }
        Unlock(null);
    }

    IEnumerator Reject(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable wrong)
    {
        yield return null;
        if (socket.interactionManager && socket.IsSelecting(wrong))
            socket.interactionManager.SelectExit(socket, wrong);
    }

    IEnumerator Seat(Transform key)
    {
        yield return null;
        var grab = key.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        var rb = key.GetComponent<Rigidbody>();
        Transform attach = socket.attachTransform ? socket.attachTransform : transform;

        if (grab && socket.interactionManager && socket.IsSelecting(grab))
            socket.interactionManager.SelectExit((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)socket, (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);

        key.SetPositionAndRotation(attach.position, attach.rotation);
        key.SetParent(transform, true);
        if (rb) rb.isKinematic = true;
        if (grab) grab.enabled = false;
        socket.socketActive = false;
    }
}
