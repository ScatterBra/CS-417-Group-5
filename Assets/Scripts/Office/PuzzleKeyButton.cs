using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// One pressable key of a <see cref="SequencePuzzle"/>. Poke it with a finger, touch it and
/// pull the trigger, or - for a player small enough to walk on the keyboard - step on it.
/// It reports its label and flashes green (right) or red (wrong).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(XRSimpleInteractable))]
public sealed class PuzzleKeyButton : MonoBehaviour
{
    public SequencePuzzle puzzle;
    public string label;

    [Tooltip("Flashes when the key is pressed. Its material should be transparent.")]
    public Renderer highlight;
    public Color acceptedColor = new Color(0.3f, 1f, 0.45f, 0.7f);
    public Color wrongColor = new Color(1f, 0.25f, 0.2f, 0.7f);

    [Min(0.05f)]
    public float flashSeconds = 0.45f;

    [Tooltip("Stepping onto the key presses it, like a floor button.")]
    public bool stepToPress = true;

    private XRSimpleInteractable interactable;
    private BoxCollider keyCollider;
    private CharacterController playerBody;
    private bool steppedOn;
    private Material highlightMaterial;
    private Color flashColor;
    private float flashTime = -1f;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        keyCollider = GetComponent<BoxCollider>();
        if (highlight != null)
        {
            highlightMaterial = highlight.material;
            highlight.enabled = false;
        }
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelected);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelected);
    }

    private void OnDestroy()
    {
        if (highlightMaterial != null)
        {
            Destroy(highlightMaterial);
        }
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        Press();
    }

    /// <summary>Press this key. Also callable from a UnityEvent while testing.</summary>
    public void Press()
    {
        if (puzzle == null)
        {
            return;
        }

        SequencePuzzle.Result result = puzzle.Submit(label);
        if (result == SequencePuzzle.Result.Ignored)
        {
            return;
        }

        flashColor = result == SequencePuzzle.Result.Wrong ? wrongColor : acceptedColor;
        flashTime = Time.time;
    }

    private void Update()
    {
        if (stepToPress)
        {
            CheckStep();
        }

        if (highlightMaterial == null || flashTime < 0f)
        {
            return;
        }

        float t = (Time.time - flashTime) / flashSeconds;
        if (t >= 1f)
        {
            highlight.enabled = false;
            flashTime = -1f;
            return;
        }

        // Ease out: bright on the press, fading away.
        Color color = flashColor;
        color.a *= 1f - t * t;
        highlightMaterial.SetColor("_BaseColor", color);
        highlight.enabled = true;
    }

    /// <summary>Press once when the player's feet land on the key; again only after stepping off.</summary>
    private void CheckStep()
    {
        if (keyCollider == null)
        {
            return;
        }

        if (playerBody == null)
        {
            XROrigin origin = FindFirstObjectByType<XROrigin>();
            if (origin == null) return;
            playerBody = origin.GetComponent<CharacterController>();
            if (playerBody == null) return;
        }

        Bounds body = playerBody.bounds;
        Vector3 feet = new Vector3(body.center.x, body.min.y, body.center.z);
        Vector3 local = transform.InverseTransformPoint(feet) - keyCollider.center;
        Vector3 half = keyCollider.size * 0.5f;

        // Standing on top: inside the key's footprint and within a finger's width above it.
        float topClearance = 0.02f / Mathf.Max(transform.lossyScale.y, 0.0001f);
        bool onKey = Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.z) <= half.z &&
                     local.y >= -half.y && local.y <= half.y + topClearance;

        if (onKey && !steppedOn)
        {
            Press();
        }

        steppedOn = onKey;
    }
}
