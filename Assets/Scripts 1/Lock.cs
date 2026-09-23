using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// Goes on an object that has an XR Socket Interactor.
/// Accepts only the Key Prop whose keyId matches requiredKeyId, then plays eased state changes.
[RequireComponent(typeof(XRSocketInteractor))]
public class Lock : MonoBehaviour
{
    [Header("Which key fits")]
    public string requiredKeyId = "";          // empty = any key fits

    [Header("When unlocked")]
    public bool lockKeyInPlace = true;         // key stays seated and can't be pulled back out
    public EasedStateChange[] stateChanges;    // door slides, bulb lights up, etc.
    public UnityEvent onUnlocked;              // e.g. Gate.Unlock(), reveal an object
    public UnityEvent onWrongKey;              // e.g. play a "nope" sound

    public bool IsUnlocked { get; private set; }

    XRSocketInteractor socket;

    void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
        socket.recycleDelayTime = 1.5f;        // stops a rejected key from instantly re-snapping
        socket.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDestroy()
    {
        if (socket) socket.selectEntered.RemoveListener(OnSelectEntered);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (IsUnlocked) return;

        Transform key = args.interactableObject.transform;
        var prop = key.GetComponent<GrabbableProp>();
        bool correct = prop != null && prop.isKey &&
                       (string.IsNullOrEmpty(requiredKeyId) || prop.keyId == requiredKeyId);

        if (!correct)
        {
            onWrongKey.Invoke();
            StartCoroutine(Reject(args.interactableObject));
            return;
        }

        IsUnlocked = true;
        foreach (var s in stateChanges) if (s) s.Play();
        onUnlocked.Invoke();
        if (GameManager.Instance) GameManager.Instance.LockSolved(this);
        if (lockKeyInPlace) StartCoroutine(Seat(key));
    }

    IEnumerator Reject(IXRSelectInteractable wrong)
    {
        yield return null;
        if (socket.interactionManager && socket.IsSelecting(wrong))
            socket.interactionManager.SelectExit(socket, wrong);
    }

    IEnumerator Seat(Transform key)
    {
        yield return null;
        var grab = key.GetComponent<XRGrabInteractable>();
        var rb = key.GetComponent<Rigidbody>();
        Transform attach = socket.attachTransform ? socket.attachTransform : transform;

        if (grab && socket.interactionManager && socket.IsSelecting(grab))
            socket.interactionManager.SelectExit((IXRSelectInteractor)socket, (IXRSelectInteractable)grab);

        key.SetPositionAndRotation(attach.position, attach.rotation);
        key.SetParent(transform, true);
        if (rb) rb.isKinematic = true;
        if (grab) grab.enabled = false;
        socket.socketActive = false;
    }
}
