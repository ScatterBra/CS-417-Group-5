using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#endif

[DisallowMultipleComponent]
public sealed class SyringeControl : MonoBehaviour
{
    public ShrinkZone shrinkZone;

    public GameObject promptRoot;


    public bool requireHeld = true;

   
    public UnityEvent onLoaded = new UnityEvent();
    public UnityEvent onUsed = new UnityEvent();

    public bool IsLoaded { get; private set; }
    public bool IsUsed { get; private set; }

    XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        if (promptRoot) promptRoot.SetActive(false);
    }

    public void LoadVial()
    {
        if (IsLoaded) return;
        IsLoaded = true;
        onLoaded.Invoke();
    }

    void Update()
    {
        if (!IsLoaded || IsUsed) return;

        bool ready = !requireHeld || (grab != null && grab.isSelected);
        if (promptRoot) promptRoot.SetActive(ready);

        if (ready && UsePressed()) Use();
    }

    void Use()
    {
        IsUsed = true;
        if (promptRoot) promptRoot.SetActive(false);
      
        if (shrinkZone) shrinkZone.Shrink(false);
        else Debug.LogWarning("[SyringeControl] No ShrinkZone assigned.", this);

        onUsed.Invoke();
    }

    static bool UsePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) return true;

        var left = XRController.leftHand;
        var x = left?.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primaryButton");
        if (x != null && x.wasPressedThisFrame) return true;
#endif
        return false;
    }
}