using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// A plug that starts in its socket. Pulling it out reports its label to a
/// <see cref="SequencePuzzle"/>. On a wrong pull every plug of the puzzle goes back in.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable))]
public sealed class PuzzlePlug : MonoBehaviour, IXRSelectFilter
{
    public SequencePuzzle puzzle;
    public string label;

    [Tooltip("The socket this plug sits in. It will only ever accept this plug.")]
    public XRSocketInteractor socket;

    private XRGrabInteractable grab;
    private bool returning;
    private bool returnPending;

    public bool canProcess => isActiveAndEnabled;

    /// <summary>True while the plug is in its socket.</summary>
    public bool isPluggedIn => socket != null && socket.hasSelection && socket.IsSelecting(grab);

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (socket != null)
        {
            socket.selectFilters.Add(this);
            socket.selectExited.AddListener(OnPulledOut);
        }

        if (puzzle != null)
        {
            puzzle.wrongStep += ReturnToSocket;
        }
    }

    private void OnDisable()
    {
        if (socket != null)
        {
            socket.selectFilters.Remove(this);
            socket.selectExited.RemoveListener(OnPulledOut);
        }

        if (puzzle != null)
        {
            puzzle.wrongStep -= ReturnToSocket;
        }
    }

    /// <inheritdoc />
    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        // The socket only ever takes its own plug.
        return !ReferenceEquals(interactor, socket) || ReferenceEquals(interactable, grab);
    }

    private void OnPulledOut(SelectExitEventArgs args)
    {
        if (returning || !ReferenceEquals(args.interactableObject, grab) || puzzle == null)
        {
            return;
        }

        SequencePuzzle.Result result = puzzle.Submit(label);
        if (result == SequencePuzzle.Result.Accepted || result == SequencePuzzle.Result.Solved)
        {
            // A plug that was pulled at the right moment stays out. Otherwise dropping it next
            // to its socket would snap it back in while the puzzle still counts it as pulled.
            socket.socketActive = false;
        }
    }

    /// <summary>Put the plug back in its socket, taking it out of the player's hand if needed.</summary>
    public void ReturnToSocket()
    {
        // A wrong pull is reported from inside XRI's own selection handling for this plug;
        // changing its selection again right there is unsafe, so do it next frame.
        returnPending = true;
    }

    private void Update()
    {
        if (!returnPending)
        {
            return;
        }

        returnPending = false;
        if (socket == null)
        {
            return;
        }

        socket.socketActive = true;
        if (isPluggedIn)
        {
            return;
        }

        returning = true;
        var manager = grab.interactionManager;
        if (grab.isSelected)
        {
            manager.CancelInteractableSelection((IXRSelectInteractable)grab);
        }

        manager.SelectEnter((IXRSelectInteractor)socket, (IXRSelectInteractable)grab);
        returning = false;
    }
}
