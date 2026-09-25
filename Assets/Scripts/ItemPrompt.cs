using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>One contextual prompt for the held collectible or nearby pointed-at item.</summary>
[DisallowMultipleComponent]
public sealed class ItemPrompt : MonoBehaviour
{
    [SerializeField] private GameObject promptRoot;
    [SerializeField, Min(0.1f)] private float interactionDistance = 2.5f;
    private Text label;
    private XRGrabInteractable grab;
    private CollectibleItem collectible;
    private static readonly List<ItemPrompt> items = new List<ItemPrompt>();
    private static ItemPrompt target;
    private static int targetFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTargets() { items.Clear(); target = null; targetFrame = -1; }

    private void OnEnable() { if (!items.Contains(this)) items.Add(this); targetFrame = -1; }
    private void OnDisable()
    {
        items.Remove(this);
        targetFrame = -1;
        if (promptRoot != null) promptRoot.SetActive(false);
    }
    private void Start() => PreparePrompt();

    public void UsePrompt(GameObject existing, float distance)
    {
        if (existing != null) promptRoot = existing;
        interactionDistance = distance;
    }

    public XRBaseInputInteractor HoldingHand()
    {
        if (grab == null) grab = GetComponent<XRGrabInteractable>();
        if (grab != null)
            foreach (var interactor in grab.interactorsSelecting)
                if (interactor is XRBaseInputInteractor hand && hand.handedness != InteractorHandedness.None)
                    return hand;
        return null;
    }

    public static ItemPrompt CurrentTarget
    {
        get
        {
            if (targetFrame == Time.frameCount) return target;
            targetFrame = Time.frameCount;
            target = null;
            var camera = Camera.main;
            float best = float.PositiveInfinity;
            foreach (var item in items)
            {
                if (item == null || !item.isActiveAndEnabled) continue;
                var hand = item.HoldingHand();
                var collect = item.GetComponent<CollectibleItem>();
                if (hand != null)
                {
                    // Prefer the right-hand item when both hands hold collectibles: B is on the right.
                    if (collect != null && collect.enabled)
                    {
                        float priority = hand.handedness == InteractorHandedness.Right ? -2 : -1;
                        if (priority < best) { target = item; best = priority; }
                    }
                    continue;
                }
                if (camera == null || (item.grab != null && !item.grab.isActiveAndEnabled)) continue;
                float distance = Vector3.Distance(camera.transform.position, item.transform.position);
                if (distance > item.interactionDistance || distance >= best) continue;
                bool pointed = false;
                if (item.grab != null)
                    foreach (var interactor in item.grab.interactorsHovering)
                        if (interactor is XRBaseInputInteractor) { pointed = true; break; }
                // Looking directly at an item also supports desktop testing; solid objects block the ray.
                if (!pointed && Physics.Raycast(camera.transform.position, camera.transform.forward,
                        out var hit, item.interactionDistance, ~0, QueryTriggerInteraction.Ignore))
                    pointed = hit.collider.GetComponentInParent<ItemPrompt>() == item;
                if (pointed) { target = item; best = distance; }
            }
            return target;
        }
    }

    private void LateUpdate()
    {
        if (promptRoot == null) return;
        bool visible = CurrentTarget == this;
        promptRoot.SetActive(visible);
        if (!visible) return;
        bool held = HoldingHand() != null;
        label.text = collectible != null
            ? (held || grab == null ? "" : "Hold Trigger to Grab\n") + $"B to Collect {collectible.scoreValue} pts"
            : "Hold Trigger to Grab";
        if (Camera.main != null) promptRoot.transform.rotation = Camera.main.transform.rotation;
    }

    // Author in the scene/prefab when possible. The fallback keeps adding a collectible script sufficient.
    [ContextMenu("Prepare Prompt")]
    public void PreparePrompt()
    {
        grab = GetComponent<XRGrabInteractable>();
        collectible = GetComponent<CollectibleItem>();
        if (promptRoot == null)
        {
            promptRoot = new GameObject(collectible != null ? "collectPrompt" : "grabPrompt", typeof(Canvas));
            promptRoot.transform.SetParent(transform, false);
            var rect = (RectTransform)promptRoot.transform;
            rect.sizeDelta = new Vector2(440, 100);
            var scale = transform.lossyScale;
            rect.localScale = new Vector3(.0015f / Mathf.Abs(scale.x), .0015f / Mathf.Abs(scale.y), .0015f / Mathf.Abs(scale.z));
            float top = transform.position.y;
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>()) top = Mathf.Max(top, renderer.bounds.max.y);
            rect.position = new Vector3(transform.position.x, top + .18f, transform.position.z);
            promptRoot.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var textObject = new GameObject("promptText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(rect, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
            label = textObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 30;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            var shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = Color.black; shadow.effectDistance = new Vector2(1, -1);
        }
        label = promptRoot.GetComponentInChildren<Text>(true);
        label.text = collectible != null ? $"Hold Trigger to Grab\nB to Collect {collectible.scoreValue} pts" : "Hold Trigger to Grab";
        promptRoot.SetActive(false);
    }
}
