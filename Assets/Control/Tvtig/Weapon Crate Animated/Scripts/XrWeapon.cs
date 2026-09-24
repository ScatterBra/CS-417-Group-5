using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(XRSimpleInteractable))]
public class XrWeaponActions : MonoBehaviour
{
    [SerializeField] private VisualEffect _visualEffect;

    private Animator _animator;
    private XRSimpleInteractable _interactable;
    private static readonly int OpenHash = Animator.StringToHash("Open");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _interactable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        // Triggers when player presses activate (e.g., XR Controller Trigger)
        _interactable.activated.AddListener(OnActivate);
        // Triggers when player stops looking/hovering or exits range
        _interactable.hoverExited.AddListener(OnHoverExit);
    }

    private void OnDisable()
    {
        _interactable.activated.RemoveListener(OnActivate);
        _interactable.hoverExited.RemoveListener(OnHoverExit);
    }

    private void OnActivate(ActivateEventArgs args)
    {
        _animator.SetBool(OpenHash, true);
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        _animator.SetBool(OpenHash, false);
    }

    // Called via Unity Animation Event
    private void OnLidLifted()
    {
        if (_visualEffect != null)
        {
            _visualEffect.SendEvent("OnPlay");
        }
    }
}