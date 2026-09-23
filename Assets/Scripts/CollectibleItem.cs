using System;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

/// <summary>
/// Makes the attached GameObject a collectible.
/// The player approaches it and presses the interaction button to collect it.
/// </summary>
[DisallowMultipleComponent]
public sealed class CollectibleItem : MonoBehaviour
{
    [Min(0)]
    [Tooltip("Points awarded when this item is collected.")]
    public int scoreValue = 1;

    private string progressId;

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
    }

    private void Awake()
    {
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
        Text promptText = promptRoot.GetComponentInChildren<Text>(true);
        if (promptText != null)
            promptText.text = $"E / A / VR Primary\nCollect +{scoreValue}";
        promptRoot.SetActive(false);
    }

    private void OnValidate()
    {
        scoreValue = Mathf.Max(0, scoreValue);
        interactionDistance = Mathf.Max(0.1f, interactionDistance);

    }

    private void Update()
    {
        bool playerInRange = IsPlayerInRange();

        if (promptRoot != null && promptRoot.activeSelf != playerInRange)
        {
            promptRoot.SetActive(playerInRange);
        }

        if (playerInRange && InteractionPressedThisFrame())
        {
            Collect();
        }
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
    /// Collects this item once. This method can also be connected directly to
    /// an XR interaction event after the team chooses the VR controller button.
    /// </summary>
    public void Collect()
    {
        if (collected)
        {
            return;
        }

        collected = true;
        gameObject.SetActive(false);
        if (!GameProgress.TryCollectItem(progressId)) return;
        TotalScore += scoreValue;
        ScoreChanged?.Invoke(TotalScore);
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
        text.text = $"Press E / A / VR Primary to collect  (+{scoreValue})";
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.75f, 1f, 1f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        promptRoot.SetActive(false);
    }

    private static bool InteractionPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            return true;
        }

        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            return true;
        }

        foreach (InputDevice device in InputSystem.devices)
        {
            ButtonControl primaryButton = device.TryGetChildControl<ButtonControl>("primaryButton");
            if (primaryButton != null && primaryButton.wasPressedThisFrame)
            {
                return true;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }
}
