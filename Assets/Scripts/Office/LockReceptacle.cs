using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// A "Lock": a Socket Interactor that accepts exactly one Key Prop.
/// Dropping the right Key in solves the Lock once and for good - the eased
/// signifier changes fire, and the Key can no longer be pulled back out.
/// Covers rubric W3 (Locks with accepting affordances) and feeds X1 (escaping).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(XRSocketInteractor))]
public sealed class LockReceptacle : MonoBehaviour, IXRSelectFilter
{
    [Tooltip("The one Key Prop this Lock accepts. Every other interactable is rejected.")]
    public XRGrabInteractable acceptedKey;

    [Tooltip("Short label shown on the exit panel, e.g. COFFEE.")]
    public string displayName = "LOCK";

    [Tooltip("Signifier changes played the moment this Lock is solved.")]
    public EasedStateChange[] solvedStateChanges = Array.Empty<EasedStateChange>();

    [Tooltip("Translucent preview of the Key sitting in this Lock. Fades out once solved.")]
    public GameObject ghostPreview;

    public UnityEvent onSolved = new UnityEvent();

    /// <summary>True once the correct Key has been seated.</summary>
    public bool isSolved { get; private set; }

    /// <summary>Raised by any Lock in the scene the moment it is solved.</summary>
    public static event Action<LockReceptacle> lockSolved;

    private XRSocketInteractor socket;

    /// <inheritdoc />
    public bool canProcess => isActiveAndEnabled;

    private void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
    }

    private void OnEnable()
    {
        socket.selectEntered.AddListener(OnSocketSelectEntered);

        // Registered on both sides: on the socket it rejects the wrong Key,
        // on the Key it stops a hand taking it back once the Lock is solved.
        socket.selectFilters.Add(this);
        if (acceptedKey != null)
        {
            acceptedKey.selectFilters.Add(this);
        }
    }

    private void OnDisable()
    {
        socket.selectEntered.RemoveListener(OnSocketSelectEntered);
        socket.selectFilters.Remove(this);
        if (acceptedKey != null)
        {
            acceptedKey.selectFilters.Remove(this);
        }
    }

    /// <inheritdoc />
    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (ReferenceEquals(interactor, socket))
        {
            // This Lock only ever closes around its own Key.
            return ReferenceEquals(interactable, acceptedKey);
        }

        // A hand (or any other interactor) reaching for a Key that is already home.
        if (ReferenceEquals(interactable, acceptedKey))
        {
            return !isSolved;
        }

        return true;
    }

    private void OnSocketSelectEntered(SelectEnterEventArgs args)
    {
        if (isSolved || !ReferenceEquals(args.interactableObject, acceptedKey))
        {
            return;
        }

        Solve();
    }

    /// <summary>Solve this Lock. Public so it can also be driven from a UnityEvent while testing.</summary>
    public void Solve()
    {
        if (isSolved)
        {
            return;
        }

        isSolved = true;

        if (acceptedKey != null)
        {
            GrabSignifier signifier = acceptedKey.GetComponent<GrabSignifier>();
            if (signifier != null)
            {
                // The Key is home, so it stops advertising that it can be picked up.
                signifier.Settle();
            }

            KeyOutlineShell outline = acceptedKey.GetComponent<KeyOutlineShell>();
            if (outline != null)
            {
                outline.SetVisible(false);
            }
        }

        if (ghostPreview != null)
        {
            EasedStateChange ghostFade = ghostPreview.GetComponent<EasedStateChange>();
            if (ghostFade != null)
            {
                ghostFade.Play();
            }
            else
            {
                ghostPreview.SetActive(false);
            }
        }

        foreach (EasedStateChange change in solvedStateChanges)
        {
            if (change != null)
            {
                change.Play();
            }
        }

        onSolved.Invoke();

        Action<LockReceptacle> handler = lockSolved;
        if (handler != null)
        {
            handler(this);
        }
    }
}
