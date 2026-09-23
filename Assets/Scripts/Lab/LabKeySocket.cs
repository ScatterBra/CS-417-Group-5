using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>Only accepts its matching access key.</summary>
public sealed class LabKeySocket : XRSocketInteractor
{
    [UnityEngine.SerializeField] private int requiredId;
    public int RequiredId { get => requiredId; set => requiredId = value; }

    public override bool CanHover(IXRHoverInteractable interactable)
    {
        return Matches(interactable.transform) && base.CanHover(interactable);
    }

    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        return Matches(interactable.transform) && base.CanSelect(interactable);
    }

    private bool Matches(UnityEngine.Transform candidate)
    {
        LabKeyToken key = candidate.GetComponentInParent<LabKeyToken>();
        return key != null && key.Id == RequiredId;
    }
}
