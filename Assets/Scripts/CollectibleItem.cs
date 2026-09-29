using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#endif

/// <summary>
/// Makes the attached GameObject a collectible.
/// Collect the prompted item using the right-hand B button.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ItemPrompt))]
public sealed class CollectibleItem : MonoBehaviour
{
    [Min(0)]
    [Tooltip("Points awarded when this item is collected.")]
    public int scoreValue = 1;

    private string progressId;
    private static int inputFrame = -1;
    private XRGrabInteractable grab;

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

        var prompt = GetComponent<ItemPrompt>();
        if (prompt == null) prompt = gameObject.AddComponent<ItemPrompt>();
        prompt.UsePrompt(promptRoot, interactionDistance);
    }

    private void OnValidate()
    {
        scoreValue = Mathf.Max(0, scoreValue);
        interactionDistance = Mathf.Max(0.1f, interactionDistance);

    }

    private void Update()
    {
        if (inputFrame == Time.frameCount) return;
        inputFrame = Time.frameCount;
        if (TryReadCollectButton(out _))
        {
            var target = ItemPrompt.CurrentTarget;
            if (target != null) target.GetComponent<CollectibleItem>()?.Collect();
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

    private static bool TryReadCollectButton(out InteractorHandedness hand)
    {
        hand = InteractorHandedness.None;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) return true;
        if (SecondaryPressed(XRController.rightHand)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static bool SecondaryPressed(XRController controller)
    {
        var button = controller?.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("secondaryButton");
        return button != null && button.wasPressedThisFrame;
    }
#endif
}
