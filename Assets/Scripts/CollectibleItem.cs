using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Makes the attached GameObject a collectible.
/// The player enters its trigger and presses the interaction button to collect it.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public sealed class CollectibleItem : MonoBehaviour
{
    [Min(0)]
    [Tooltip("Points awarded when this item is collected.")]
    public int scoreValue = 1;

    public static int TotalScore { get; private set; }
    public static event Action<int> ScoreChanged;

    private bool playerInRange;
    private bool collected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetScore()
    {
        TotalScore = 0;
    }

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;

        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnValidate()
    {
        scoreValue = Mathf.Max(0, scoreValue);

        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }

    private void Update()
    {
        if (playerInRange && InteractionPressedThisFrame())
        {
            Collect();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (BelongsToPlayer(other))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (BelongsToPlayer(other))
        {
            playerInRange = false;
        }
    }

    /// <summary>
    /// Collects this item once. This method can later be connected directly to
    /// an XR interaction event after the team chooses the VR controller button.
    /// </summary>
    public void Collect()
    {
        if (collected)
        {
            return;
        }

        collected = true;
        TotalScore += scoreValue;
        ScoreChanged?.Invoke(TotalScore);
        gameObject.SetActive(false);
    }

    private static bool BelongsToPlayer(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            return true;
        }

        Camera mainCamera = Camera.main;
        return mainCamera != null && other.transform.root == mainCamera.transform.root;
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
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }
}
