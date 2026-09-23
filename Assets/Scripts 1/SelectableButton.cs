using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// A physical 3D button the player presses with the hand's grip (or trigger) via a Direct Interactor.
/// Add to a cube/pad that has a Collider + XR Simple Interactable.
/// Use for Start, Restart, Puzzle buttons, etc. Wire actions in "On Pressed".
[RequireComponent(typeof(XRSimpleInteractable))]
public class SelectableButton : MonoBehaviour
{
    public UnityEvent onPressed;
    public float cooldown = 0.5f;
    public EscapeEase pressAnimation;   // optional: button sinks in, then pops back

    float lastPress = -99f;

    void Awake()
    {
        var i = GetComponent<XRSimpleInteractable>();
        i.selectEntered.AddListener(_ => Press());
        i.activated.AddListener(_ => Press());
    }

    void Press()
    {
        if (Time.time - lastPress < cooldown) return;
        lastPress = Time.time;
        onPressed.Invoke();
        if (pressAnimation) StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        pressAnimation.Play();
        yield return new WaitForSeconds(pressAnimation.duration + 0.1f);
        pressAnimation.Reverse();
    }
}
