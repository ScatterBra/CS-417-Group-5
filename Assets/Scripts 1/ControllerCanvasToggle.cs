using UnityEngine;
using UnityEngine.XR;

/// Press a controller button to show/hide a UI Canvas (or any object). Starts hidden.
public class ControllerCanvasToggle : MonoBehaviour
{
    public GameObject canvasToToggle;

    public XRNode controllerHand = XRNode.LeftHand;   // pick the hand not already used for Restart

    public enum ControllerButton { Primary, Secondary, Grip, Trigger }
    [Tooltip("Primary = A (right) / X (left). Secondary = B (right) / Y (left) on Quest.")]
    public ControllerButton button = ControllerButton.Secondary;

    bool wasPressed;

    void Awake()
    {
        if (canvasToToggle) canvasToToggle.SetActive(false);
    }

    void Update()
    {
        var device = InputDevices.GetDeviceAtXRNode(controllerHand);
        var usage = button switch
        {
            ControllerButton.Primary => CommonUsages.primaryButton,
            ControllerButton.Grip => CommonUsages.gripButton,
            ControllerButton.Trigger => CommonUsages.triggerButton,
            _ => CommonUsages.secondaryButton
        };

        bool pressed = device.TryGetFeatureValue(usage, out bool val) && val;

        if (pressed && !wasPressed) Toggle();   // fire once per press
        wasPressed = pressed;
    }

    public void Toggle()
    {
        if (canvasToToggle) canvasToToggle.SetActive(!canvasToToggle.activeSelf);
    }
}
