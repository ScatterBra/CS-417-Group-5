using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#endif

/// <summary>
/// Makes the attached GameObject a collectible.
/// Collect the nearest item, or the item held by the hand pressing its primary button.
/// </summary>
[DisallowMultipleComponent]
public sealed class CollectibleItem : MonoBehaviour
{
    [Min(0)]
    [Tooltip("Points awarded when this item is collected.")]
    public int scoreValue = 1;

    private string progressId;
    private static readonly List<CollectibleItem> activeItems = new List<CollectibleItem>();
    private static int inputFrame = -1;
    private XRGrabInteractable grab;
    private Text promptText;

    [SerializeField, HideInInspector, Min(0.1f)]
    [Tooltip("How close the player's camera must be before the collect prompt appears.")]
    private float interactionDistance = 2.5f;

    public static int TotalScore { get; private set; }
    public static event Action<int> ScoreChanged;

    private bool collected;
    [SerializeField, HideInInspector, Tooltip("Optional prebuilt prompt Canvas under this item.")]
    private GameObject promptRoot;
    private Camera playerCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetScore()
    {
        TotalScore = 0;
        activeItems.Clear();
        inputFrame = -1;
    }

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        // Cache the authored location before gameplay can move or hide this object.
        // Sibling indices distinguish copies, including objects with identical names.
        string itemId = "";
        for (Transform current = transform; current != null; current = current.parent)
            itemId = current.name + "[" + current.GetSiblingIndex() + "]/" + itemId;
        progressId = gameObject.scene.path + "::" + itemId;
        if (GameProgress.IsItemCollected(progressId))
        {
            collected = true;
            gameObject.SetActive(false);
            return;
        }

        if (promptRoot == null) BuildPrompt();
        promptText = promptRoot.GetComponentInChildren<Text>(true);
        if (promptText != null)
            promptText.text = $"A / X / E: Collect +{scoreValue}";
        promptRoot.SetActive(false);
    }

    private void OnValidate()
    {
        scoreValue = Mathf.Max(0, scoreValue);
        interactionDistance = Mathf.Max(0.1f, interactionDistance);

    }

    private void OnEnable()
    {
        if (!activeItems.Contains(this)) activeItems.Add(this);
    }

    private void OnDisable() => activeItems.Remove(this);

    private void Update()
    {
        XRBaseInputInteractor hand = HoldingHand();
        bool isTarget = this == FindTarget(InteractorHandedness.None) ||
                        this == FindTarget(InteractorHandedness.Left) ||
                        this == FindTarget(InteractorHandedness.Right);
        if (promptText != null)
        {
            string button = hand == null ? "A / X / E" :
                hand.handedness == InteractorHandedness.Left ? "X" : "A";
            promptText.text = $"{button}: Collect +{scoreValue}";
            if (hand == null && GetComponent<GrabbableItem>() != null)
                promptText.text += "\nHold Trigger to grab";
        }
        if (promptRoot != null) promptRoot.SetActive(isTarget);

        // All items share this input pass: one press can never collect a whole cluster.
        if (inputFrame == Time.frameCount) return;
        inputFrame = Time.frameCount;
        if (TryReadCollectButton(out InteractorHandedness pressedHand))
            FindTarget(pressedHand)?.Collect();
    }

    private static CollectibleItem FindTarget(InteractorHandedness hand)
    {
        CollectibleItem nearest = null;
        float nearestDistance = float.PositiveInfinity;
        Camera camera = Camera.main;
        foreach (CollectibleItem item in activeItems)
        {
            if (item == null || item.collected || !item.isActiveAndEnabled) continue;
            XRBaseInputInteractor holder = item.HoldingHand();
            if (holder != null)
            {
                if (hand == InteractorHandedness.None || holder.handedness == hand) return item;
                continue; // The other hand's held item is not a proximity target.
            }
            if (camera == null) continue;
            float distance = Vector3.Distance(camera.transform.position, item.transform.position);
            if (distance <= item.interactionDistance && distance < nearestDistance)
            {
                nearest = item;
                nearestDistance = distance;
            }
        }
        return nearest;
    }

    private void LateUpdate()
    {
        if (promptRoot == null || !promptRoot.activeSelf)
        {
            return;
        }

        Camera camera = GetPlayerCamera();
        if (camera != null)
        {
            promptRoot.transform.rotation = camera.transform.rotation;
        }
    }

    /// <summary>
    /// Collects this item once while nearby or held; also usable by interaction events.
    /// </summary>
    public void Collect()
    {
        if (collected || (HoldingHand() == null && !IsPlayerInRange()))
        {
            return;
        }

        collected = true;
        gameObject.SetActive(false);
        if (!GameProgress.TryCollectItem(progressId)) return;
        TotalScore += scoreValue;
        ScoreChanged?.Invoke(TotalScore);
    }

    private XRBaseInputInteractor HoldingHand()
    {
        if (grab == null) grab = GetComponent<XRGrabInteractable>();
        if (grab == null) return null;
        foreach (var interactor in grab.interactorsSelecting)
            if (interactor is XRBaseInputInteractor hand &&
                hand.handedness != InteractorHandedness.None) return hand;
        return null;
    }

    private bool IsPlayerInRange()
    {
        Camera camera = GetPlayerCamera();
        return camera != null &&
               Vector3.Distance(camera.transform.position, transform.position) <= interactionDistance;
    }

    private Camera GetPlayerCamera()
    {
        if (playerCamera == null || !playerCamera.isActiveAndEnabled)
        {
            playerCamera = Camera.main;
        }

        return playerCamera;
    }

    private void BuildPrompt()
    {
        promptRoot = new GameObject("Collect Prompt", typeof(RectTransform), typeof(Canvas));
        promptRoot.transform.SetParent(transform, false);
        promptRoot.transform.localPosition = Vector3.up * 0.75f;
        promptRoot.transform.localScale = Vector3.one * 0.0025f;

        RectTransform promptRect = (RectTransform)promptRoot.transform;
        promptRect.sizeDelta = new Vector2(420f, 100f);

        Canvas canvas = promptRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(promptRoot.transform, false);
        RectTransform backgroundRect = (RectTransform)background.transform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0.02f, 0.08f, 0.13f, 0.9f);

        GameObject label = new GameObject("Instruction", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(background.transform, false);
        RectTransform labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(14f, 8f);
        labelRect.offsetMax = new Vector2(-14f, -8f);

        Text text = label.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "Hold Trigger to grab";
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.75f, 1f, 1f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        promptRoot.SetActive(false);
    }

    private static bool TryReadCollectButton(out InteractorHandedness hand)
    {
        hand = InteractorHandedness.None;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
        if (PrimaryPressed(XRController.rightHand)) { hand = InteractorHandedness.Right; return true; }
        if (PrimaryPressed(XRController.leftHand)) { hand = InteractorHandedness.Left; return true; }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static bool PrimaryPressed(XRController controller)
    {
        var button = controller?.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primaryButton");
        return button != null && button.wasPressedThisFrame;
    }
#endif
}
