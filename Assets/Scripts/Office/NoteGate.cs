using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// A sticky note that is also a gate. Walk up to it and a prompt appears; hold the button
/// and you are teleported next to the next note in the chain.
///
/// Notes are chained by the number on them, which comes from the object name:
/// "VV_Notes_1" is note 1, "VV_Notes_1 (1)" is note 2, and so on. The last note leads back
/// to the first. The landing spot is found automatically on whatever surface sits in front
/// of and below the destination note, so moving a note moves where it sends people.
/// </summary>
[DisallowMultipleComponent]
public sealed class NoteGate : MonoBehaviour
{
    [Tooltip("Place in the chain, from 0. -1 reads it from the name: 'VV_Notes_1' is 0, 'VV_Notes_1 (3)' is 3.")]
    public int order = -1;

    [Tooltip("Where this note sends the player. Empty = the next note in the chain.")]
    public NoteGate destinationOverride;

    [Tooltip("Exact spot to land on in front of this note. Empty = found automatically.")]
    public Transform landingOverride;

    [Min(0.1f)]
    [Tooltip("How close the player's head must be for the prompt to appear, in the player's own " +
             "metres - it shrinks with the player.")]
    public float promptRange = 2.5f;

    [Min(0.1f)]
    public float holdSeconds = 1f;

    public XRNode controllerHand = XRNode.LeftHand;
    public Key keyboardKey = Key.T;

    [Tooltip("Shown in the prompt as 'Hold <label>'.")]
    public string buttonLabel = "X";

    [Tooltip("The note breathes a soft glow so players can tell it is more than paper.")]
    public bool glow = true;
    public Color glowColor = new Color(1f, 0.85f, 0.35f, 1f);

    public UnityEvent onTeleportedFromHere = new UnityEvent();

    /// <summary>1-based number written on the note.</summary>
    public int number => ResolvedOrder + 1;

    // A teleport only fires on a fresh press, never on a button still held from the last one.
    private static bool waitForRelease;

    // Landing clearance, sized for the shrunken player in the small room.
    private const float ClearanceRadius = 0.025f;
    private const float ClearanceHeight = 0.2f;
    private const float LandingStepIn = 0.06f;

    private int resolvedOrder = int.MinValue;
    private NoteGate next;
    private Collider ownCollider;
    private Renderer ownRenderer;
    private Vector3 front;
    private Vector3 landing;
    private bool landingFound;

    private XROrigin origin;
    private TeleportationProvider teleporter;

    private GameObject prompt;
    private CanvasGroup promptGroup;
    private Text promptTitle;
    private Text promptHint;
    private Image promptFill;
    private GameObject numberLabel;
    private Material glowMaterial;

    private float held;

    private int ResolvedOrder
    {
        get
        {
            if (resolvedOrder == int.MinValue)
            {
                resolvedOrder = order >= 0 ? order : OrderFromName(name);
            }

            return resolvedOrder;
        }
    }

    private void Awake()
    {
        ownCollider = GetComponent<Collider>();
        ownRenderer = GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        front = FacingDirection();
        landingFound = TryFindLanding(out landing);
        if (!landingFound)
        {
            Debug.LogWarning($"Note {number}: no free surface in front of it to land on. Add a Landing Override.", this);
        }

        next = destinationOverride != null ? destinationOverride : NextInChain();
        BuildNumberLabel();
        BuildPrompt();

        if (glow && ownRenderer != null)
        {
            glowMaterial = ownRenderer.material;
            glowMaterial.EnableKeyword("_EMISSION");
            glowMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
    }

    private void OnDestroy()
    {
        if (prompt != null) Destroy(prompt);
        if (numberLabel != null) Destroy(numberLabel);
        if (glowMaterial != null) Destroy(glowMaterial);
    }

    private void Update()
    {
        if (origin == null)
        {
            origin = FindFirstObjectByType<XROrigin>();
            teleporter = FindFirstObjectByType<TeleportationProvider>();
            if (origin == null) return;
        }

        float playerScale = origin.transform.lossyScale.y;
        Vector3 head = origin.Camera.transform.position;
        bool near = next != null && next.landingFound &&
                    Vector3.Distance(head, NoteCenter) <= promptRange * playerScale;

        bool pressed = ButtonHeld();
        if (!pressed)
        {
            waitForRelease = false;
        }

        held = near && pressed && !waitForRelease ? held + Time.deltaTime : 0f;
        if (held >= holdSeconds)
        {
            held = 0f;
            Teleport();
        }

        UpdateGlow(near);
        UpdatePrompt(near, head, playerScale);
    }

    /// <summary>Teleport the player to the next note now. Also callable from a UnityEvent.</summary>
    public void Teleport()
    {
        if (next == null || !next.landingFound)
        {
            return;
        }

        if (teleporter == null)
        {
            teleporter = FindFirstObjectByType<TeleportationProvider>();
            if (teleporter == null) return;
        }

        // Arrive facing the destination note, so the way on is right in front of you.
        Vector3 facing = Vector3.ProjectOnPlane(-next.front, Vector3.up);
        teleporter.QueueTeleportRequest(new TeleportRequest
        {
            destinationPosition = next.landing,
            destinationRotation = Quaternion.LookRotation(facing.sqrMagnitude > 0f ? facing : Vector3.forward, Vector3.up),
            matchOrientation = MatchOrientation.TargetUpAndForward,
            requestTime = Time.time,
        });

        waitForRelease = true;
        onTeleportedFromHere.Invoke();
    }

    // ------------------------------------------------------------------ chain

    private NoteGate NextInChain()
    {
        var gates = new List<NoteGate>(FindObjectsByType<NoteGate>());
        gates.Sort((a, b) => a.ResolvedOrder.CompareTo(b.ResolvedOrder));

        foreach (NoteGate gate in gates)
        {
            if (gate.ResolvedOrder > ResolvedOrder) return gate;
        }

        // Past the last note: back to the first one.
        return gates.Count > 0 && gates[0] != this ? gates[0] : null;
    }

    private static int OrderFromName(string objectName)
    {
        Match match = Regex.Match(objectName, @"\((\d+)\)\s*$");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    // ---------------------------------------------------------------- landing

    private Vector3 NoteCenter => ownRenderer != null ? ownRenderer.bounds.center : transform.position;

    /// <summary>The point on the paper's front face, not its middle: the note has thickness.</summary>
    private Vector3 FrontFace(float height)
    {
        Bounds bounds = ownRenderer != null ? ownRenderer.bounds : new Bounds(transform.position, Vector3.zero);
        float halfThickness = Mathf.Abs(Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(front.x), 0f, Mathf.Abs(front.z))));
        Vector3 point = bounds.center + front * halfThickness;
        point.y = Mathf.Clamp(height, bounds.min.y, bounds.max.y);
        return point;
    }

    /// <summary>The side of the note that faces open space - away from the wall it is stuck to.</summary>
    private Vector3 FacingDirection()
    {
        Bounds bounds = ownRenderer != null ? ownRenderer.bounds : new Bounds(transform.position, Vector3.one * 0.01f);
        Vector3 axis = bounds.size.x < bounds.size.z ? Vector3.right : Vector3.forward;

        float freeAlong = FreeDistance(bounds.center, axis);
        float freeAgainst = FreeDistance(bounds.center, -axis);
        return freeAlong >= freeAgainst ? axis : -axis;
    }

    private float FreeDistance(Vector3 from, Vector3 direction)
    {
        const float probe = 1f;
        foreach (RaycastHit hit in SortedHits(from, direction, probe))
        {
            if (!IsIgnored(hit.collider)) return hit.distance;
        }

        return probe;
    }

    /// <summary>
    /// Search outwards from the note for the highest surface below it with room for a small
    /// player to stand - the platform the note is stuck above.
    /// </summary>
    private bool TryFindLanding(out Vector3 point)
    {
        if (landingOverride != null)
        {
            point = landingOverride.position;
            return true;
        }

        point = default;
        Vector3 center = NoteCenter;
        var candidates = new List<(float distance, Vector3 point)>();

        for (float distance = 0.05f; distance <= 0.6f; distance += 0.025f)
        {
            Vector3 from = center + front * distance + Vector3.up * 0.05f;
            foreach (RaycastHit hit in SortedHits(from, Vector3.down, 3f))
            {
                if (IsIgnored(hit.collider)) continue;

                if (hit.point.y <= center.y && HasClearance(hit.point))
                {
                    candidates.Add((distance, hit.point));
                }

                // Only the first real surface under this probe counts.
                break;
            }
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        // The platform is the highest surface found under the note.
        float top = float.NegativeInfinity;
        foreach (var candidate in candidates) top = Mathf.Max(top, candidate.point.y);
        candidates.RemoveAll(c => c.point.y < top - 0.01f);

        // Land a step in from the platform's near edge rather than nose-to-note, so a small
        // player arrives seeing the whole note instead of a wall of yellow paper.
        float wanted = candidates[0].distance + LandingStepIn;
        point = candidates[candidates.Count - 1].point;
        foreach (var candidate in candidates)
        {
            if (candidate.distance >= wanted)
            {
                point = candidate.point;
                break;
            }
        }

        return true;
    }

    private bool HasClearance(Vector3 feet)
    {
        Vector3 bottom = feet + Vector3.up * (ClearanceRadius + 0.005f);
        Vector3 top = feet + Vector3.up * ClearanceHeight;
        foreach (Collider other in Physics.OverlapCapsule(bottom, top, ClearanceRadius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!IsIgnored(other)) return false;
        }

        return true;
    }

    private bool IsIgnored(Collider other)
    {
        if (other == null || other.isTrigger || other == ownCollider) return true;

        // The player's own body never blocks a landing.
        XROrigin player = origin != null ? origin : FindFirstObjectByType<XROrigin>();
        return player != null && other.transform.IsChildOf(player.transform);
    }

    private static RaycastHit[] SortedHits(Vector3 from, Vector3 direction, float distance)
    {
        RaycastHit[] hits = Physics.RaycastAll(from, direction, distance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        return hits;
    }

    // ------------------------------------------------------------------ input

    private bool ButtonHeld()
    {
        if (Keyboard.current != null && keyboardKey != Key.None && Keyboard.current[keyboardKey].isPressed)
        {
            return true;
        }

        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(controllerHand);
        return device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool pressed) && pressed;
    }

    // ---------------------------------------------------------------- visuals

    private void UpdateGlow(bool near)
    {
        if (glowMaterial == null) return;

        float breath = (Mathf.Sin(Time.time * 2.5f) + 1f) * 0.5f;
        float intensity = near ? Mathf.Lerp(0.6f, 1.2f, breath) : Mathf.Lerp(0.05f, 0.35f, breath);
        glowMaterial.SetColor("_EmissionColor", glowColor * intensity);
    }

    private void UpdatePrompt(bool near, Vector3 head, float playerScale)
    {
        if (prompt == null) return;

        float target = near ? 1f : 0f;
        promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, target, Time.deltaTime * 4f);
        bool visible = promptGroup.alpha > 0.001f;
        if (prompt.activeSelf != visible) prompt.SetActive(visible);
        if (!visible) return;

        // On the paper's face at the player's eye height, just proud of it, facing them.
        Vector3 position = FrontFace(head.y) + front * (0.02f * playerScale);
        prompt.transform.position = position;
        prompt.transform.rotation = Quaternion.LookRotation(position - head, Vector3.up);
        // 25 cm wide in the player's own metres: 2.5 cm for a player at one tenth scale.
        prompt.transform.localScale = Vector3.one * (0.25f / 360f * playerScale);

        promptTitle.text = next != null ? $"{number}  →  {next.number}" : number.ToString();
        promptHint.text = waitForRelease ? "Release, then hold " + buttonLabel : "Hold " + buttonLabel + " to go";
        promptFill.fillAmount = Mathf.Clamp01(held / holdSeconds);
    }

    private void BuildNumberLabel()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        numberLabel = new GameObject($"Note {number} Label", typeof(RectTransform), typeof(Canvas));
        numberLabel.transform.position = FrontFace(NoteCenter.y) + front * 0.002f;

        // A world-space canvas reads from its -Z side, so point +Z into the wall.
        numberLabel.transform.rotation = Quaternion.LookRotation(-front, Vector3.up);
        numberLabel.transform.localScale = Vector3.one * 0.0006f;
        ((RectTransform)numberLabel.transform).sizeDelta = new Vector2(200f, 200f);
        numberLabel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

        Text text = NewText(numberLabel.transform, "Number", font, 150, new Color(0.12f, 0.12f, 0.2f, 0.9f));
        text.text = number.ToString();
        text.fontStyle = FontStyle.Bold;
    }

    private void BuildPrompt()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        prompt = new GameObject($"Note {number} Prompt", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        ((RectTransform)prompt.transform).sizeDelta = new Vector2(360f, 170f);
        Canvas canvas = prompt.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 60;
        promptGroup = prompt.GetComponent<CanvasGroup>();
        promptGroup.alpha = 0f;

        Image background = NewImage(prompt.transform, "Background", new Color(0.05f, 0.07f, 0.1f, 0.88f));
        Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        promptTitle = NewText(prompt.transform, "Title", font, 64, new Color(1f, 0.88f, 0.45f, 1f));
        Stretch(promptTitle.rectTransform, new Vector2(0f, 0.45f), Vector2.one, Vector2.zero, Vector2.zero);

        promptHint = NewText(prompt.transform, "Hint", font, 34, new Color(0.85f, 0.95f, 1f, 1f));
        Stretch(promptHint.rectTransform, new Vector2(0f, 0.12f), new Vector2(1f, 0.45f), Vector2.zero, Vector2.zero);

        Image track = NewImage(prompt.transform, "Track", new Color(1f, 1f, 1f, 0.15f));
        Stretch(track.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.1f), Vector2.zero, Vector2.zero);

        promptFill = NewImage(track.transform, "Fill", new Color(1f, 0.85f, 0.35f, 1f));
        promptFill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        promptFill.type = Image.Type.Filled;
        promptFill.fillMethod = Image.FillMethod.Horizontal;
        promptFill.fillAmount = 0f;
        Stretch(promptFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        prompt.SetActive(false);
    }

    private static Text NewText(Transform parent, string objectName, Font font, int size, Color color)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return text;
    }

    private static Image NewImage(Transform parent, string objectName, Color color)
    {
        var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private void OnDrawGizmosSelected()
    {
        ownCollider = GetComponent<Collider>();
        ownRenderer = GetComponentInChildren<Renderer>();
        front = FacingDirection();
        if (TryFindLanding(out Vector3 spot))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spot + Vector3.up * 0.02f, 0.02f);
            Gizmos.DrawLine(NoteCenter, spot);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(NoteCenter, 0.05f);
        }
    }
}
