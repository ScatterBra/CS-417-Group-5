using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// A lever/button/statue that reports "I was pressed" to its PuzzleSequence.
[RequireComponent(typeof(XRSimpleInteractable))]
public class PuzzleInput : MonoBehaviour
{
    public int inputId = 1;
    public PuzzleSequence sequence;            // auto-found in parents if left empty
    public EscapeEase pressVisual;       // e.g. lever rotates down

    void Awake()
    {
        if (!sequence) sequence = GetComponentInParent<PuzzleSequence>();
        GetComponent<XRSimpleInteractable>().selectEntered.AddListener(_ =>
        {
            if (pressVisual) pressVisual.Play();
            if (sequence) sequence.Press(inputId);
        });
    }

    public void ResetVisual()
    {
        if (pressVisual) pressVisual.Reverse();
    }
}
