using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

/// <summary>Hold a button near a door to load another scene.</summary>
[DisallowMultipleComponent]
public sealed class SceneLoader : MonoBehaviour
{
    public string targetSceneName;
    public bool isUnlocked = true;
    public bool requiresPuzzleCompletion;
    [Tooltip("Unique room ID. Leave blank to use this scene's path.")]
    public string roomId;
    public string RoomId => string.IsNullOrWhiteSpace(roomId) ? gameObject.scene.path : roomId.Trim();
    private bool CanTravel => isUnlocked && (!requiresPuzzleCompletion || GameProgress.IsRoomCompleted(RoomId));
    public Transform interactionPoint;
    public GameObject promptRoot;
    public Text promptText;
    [SerializeField] private bool showLockedPrompt = true;
    [Min(0.1f)] public float interactionDistance = 2f;
    [Min(0.1f)] public float holdDuration = 1.5f;
    public Key keyboardKey = Key.E;
    public XRNode controllerHand = XRNode.LeftHand;
    public enum ControllerButton { Primary, Secondary, Grip, Trigger }
    public ControllerButton controllerButton = ControllerButton.Secondary;
    public string buttonLabel = "Y (Left Hand) / E";

    private float heldTime;
    private bool isLoading;
    private bool waitForRelease;
    private string loadError;

    private void Awake() { if (promptRoot != null) promptRoot.SetActive(false); }

    private void Update()
    {
        Camera camera = Camera.main;
        Transform point = interactionPoint != null ? interactionPoint : transform;
        bool nearby = camera != null && Vector3.Distance(camera.transform.position, point.position) <= interactionDistance;
        if (promptRoot != null) promptRoot.SetActive(nearby && (CanTravel || showLockedPrompt));
        bool held = IsHeld();
        if (!held) { waitForRelease = false; loadError = null; }
        if (!nearby || !CanTravel || !held || isLoading || waitForRelease) heldTime = 0f;
        else
        {
            heldTime += Time.deltaTime;
            if (heldTime >= Mathf.Max(0.1f, holdDuration))
            {
                waitForRelease = true;
                LoadTargetScene();
            }
        }
        if (nearby && promptText != null)
        {
            promptText.text = !CanTravel ? "Complete this room's puzzle first" : isLoading ? "Loading..." :
                loadError ?? $"Hold {buttonLabel} to travel\n{Mathf.Clamp01(heldTime / Mathf.Max(0.1f, holdDuration)):P0}";
        }
        if (nearby && promptRoot != null) promptRoot.transform.rotation = camera.transform.rotation;
    }

    private bool IsHeld()
    {
        if (Keyboard.current != null && keyboardKey != Key.None && Keyboard.current[keyboardKey].isPressed) return true;
        var device = InputDevices.GetDeviceAtXRNode(controllerHand);
        var usage = controllerButton switch
        {
            ControllerButton.Secondary => UnityEngine.XR.CommonUsages.secondaryButton,
            ControllerButton.Grip => UnityEngine.XR.CommonUsages.gripButton,
            ControllerButton.Trigger => UnityEngine.XR.CommonUsages.triggerButton,
            _ => UnityEngine.XR.CommonUsages.primaryButton
        };
        return device.TryGetFeatureValue(usage, out bool pressed) && pressed;
    }

    public void SetUnlocked(bool value) { isUnlocked = value; heldTime = 0f; }

    private void OnDisable()
    {
        heldTime = 0f;
        if (promptRoot != null) promptRoot.SetActive(false);
    }

    [ContextMenu("Load Target Scene (Play Mode)")]
    public void LoadTargetScene()
    {
        if (!Application.isPlaying || isLoading || !CanTravel) return;
        string sceneName = targetSceneName?.Trim();
        if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            loadError = "Destination unavailable";
            Debug.LogWarning("Set Target Scene Name and enable that scene in Build Profiles.", this);
            return;
        }
        isLoading = true;
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }
}

