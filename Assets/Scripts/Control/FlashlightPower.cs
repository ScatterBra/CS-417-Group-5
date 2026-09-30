using System.Collections;
using UnityEngine;

/// Put on the flashlight (same object as BlacklightProp). Starts powered OFF.
/// Wire a battery Lock's "On Unlocked ()" to FlashlightPower.PowerOn, with the Inspector's
/// float argument set to 30, and the flashlight's real light + its blacklight reveal ability
/// both turn on for that many seconds, then switch off automatically.
public class FlashlightPower : MonoBehaviour
{
    [Tooltip("The flashlight's actual beam/bulb Light. Left off until powered.")]
    public Light flashlightLight;

    [Tooltip("The BlacklightProp on this same object. Disabled while unpowered so it can't reveal writing.")]
    public BlacklightProp blacklight;

    [Tooltip("Optional: a bulb/lens Renderer to relight, using MaterialPropertyBlock (doesn't create a material leak).")]
    public Renderer bulbRenderer;
    [ColorUsage(false, true)] public Color bulbOnEmission = Color.white;

    public AudioSource powerOnSound;
    public AudioSource powerOffSound;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock mpb;
    Coroutine routine;

    public bool IsPowered { get; private set; }
    public float SecondsLeft { get; private set; }

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
        SetPowered(false);
    }

    /// Call from a UnityEvent (e.g. Lock > On Unlocked). Set the float argument in the
    /// Inspector to how many seconds of power this battery gives (e.g. 30).
    public void PowerOn(float duration)
    {
        Debug.Log($"[FlashlightPower] {name}: PowerOn({duration}) called.", this);
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PowerRoutine(duration));
    }

    IEnumerator PowerRoutine(float duration)
    {
        SetPowered(true);
        if (powerOnSound) powerOnSound.Play();

        SecondsLeft = duration;
        while (SecondsLeft > 0f)
        {
            SecondsLeft -= Time.deltaTime;
            yield return null;
        }

        SetPowered(false);
        if (powerOffSound) powerOffSound.Play();
        routine = null;
    }

    void SetPowered(bool on)
    {
        IsPowered = on;

        if (flashlightLight) flashlightLight.enabled = on;
        else Debug.LogWarning($"[FlashlightPower] {name}: Flashlight Light is not assigned.", this);

        if (blacklight) blacklight.enabled = on;   // disabling stops its Update loop entirely
        else Debug.LogWarning($"[FlashlightPower] {name}: Blacklight is not assigned.", this);

        if (bulbRenderer)
        {
            bulbRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionId, on ? bulbOnEmission : Color.black);
            bulbRenderer.SetPropertyBlock(mpb);
        }
    }
}
