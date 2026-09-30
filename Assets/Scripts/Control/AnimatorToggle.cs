using UnityEngine;

/// Lets UnityEvents drive an Animator (closet doors, drawers, hatches):
///   Lock > On Unlocked  ->  AnimatorToggle.Open
///   SelectableButton > On Pressed  ->  AnimatorToggle.Toggle
/// Pick the mode that matches how the door's Animator Controller is built.
public class AnimatorToggle : MonoBehaviour
{
    public enum Mode { Bool, Trigger, PlayState }

    public Animator animator;                    // auto-found on this object or its children
    public Mode mode = Mode.Bool;
    [Tooltip("Bool: parameter name. Trigger: open trigger. PlayState: open state name.")]
    public string openName = "Open";
    [Tooltip("Trigger: close trigger. PlayState: close state name. (Bool mode ignores this.)")]
    public string closeName = "Close";

    bool isOpen;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!animator) Debug.LogWarning($"{name}: no Animator found on this object or its children.", this);
    }

    public void Open()  { isOpen = true;  Apply(true); }
    public void Close() { isOpen = false; Apply(false); }
    public void Toggle() { isOpen = !isOpen; Apply(isOpen); }

    [ContextMenu("Test: Toggle")]
    void TestToggle()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Enter Play mode first."); return; }
        Toggle();
    }

    void Apply(bool open)
    {
        if (!animator)
        {
            Debug.LogWarning($"[AnimatorToggle] {name}: no Animator assigned or found. Drag the cabinet into the Animator slot.", this);
            return;
        }
        if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[AnimatorToggle] {name}: the Animator is disabled or has no Controller.", this);
            return;
        }

        Debug.Log($"[AnimatorToggle] {name}: asking '{animator.gameObject.name}' to {(open ? "OPEN" : "CLOSE")} (mode {mode})", this);

        switch (mode)
        {
            case Mode.Bool:
                if (HasParam(openName, AnimatorControllerParameterType.Bool))
                    animator.SetBool(openName, open);
                break;

            case Mode.Trigger:
                string trig = open ? openName : closeName;
                if (HasParam(trig, AnimatorControllerParameterType.Trigger))
                    animator.SetTrigger(trig);
                break;

            case Mode.PlayState:
                string state = open ? openName : closeName;
                if (animator.HasState(0, Animator.StringToHash(state))) animator.Play(state, 0, 0f);
                else Debug.LogWarning($"{name}: Animator has no state named '{state}'.", this);
                break;
        }
    }

    bool HasParam(string paramName, AnimatorControllerParameterType type)
    {
        foreach (var p in animator.parameters)
            if (p.name == paramName && p.type == type) return true;

        Debug.LogWarning($"{name}: Animator has no {type} parameter named '{paramName}'. " +
                         "Open Window > Animation > Animator and check the Parameters tab (names are case-sensitive).", this);
        return false;
    }
}