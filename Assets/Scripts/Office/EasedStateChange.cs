using System.Collections;
using UnityEngine;

/// <summary>
/// A one-shot, eased transition used by every signifier in the escape room.
/// Covers rubric U1: when a Lock is triggered its signifier changes to a new
/// state, and that change is eased in and out rather than snapping.
/// </summary>
[DisallowMultipleComponent]
public sealed class EasedStateChange : MonoBehaviour
{
    [Header("Timing")]
    [Min(0f)]
    [Tooltip("Seconds to wait before the change starts. Stagger these to make a cascade read clearly.")]
    public float delay;

    [Min(0.01f)]
    public float duration = 0.9f;

    [Tooltip("Flat at both ends, fast through the middle: this is the ease in / ease out.")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Transform")]
    public bool movesLocally;
    public Vector3 targetLocalPosition;
    public bool rotatesLocally;
    public Vector3 targetLocalEuler;
    public bool scales;
    public Vector3 targetLocalScale = Vector3.one;

    [Min(1f)]
    [Tooltip("Overshoot part-way through the scale change so the object 'pops'. 1 = no pop.")]
    public float scalePop = 1f;

    [Header("Colour (first renderer on this object or its children)")]
    public bool recolors;
    public Color targetBaseColor = Color.green;
    public Color targetEmissionColor = Color.green;
    [Min(0f)]
    public float targetEmissionIntensity = 2f;

    [Header("Light on this object")]
    public bool relights;
    public Color targetLightColor = Color.green;
    [Min(0f)]
    public float targetLightIntensity = 2f;

    [Header("Fade")]
    [Tooltip("Fade the renderer's alpha to zero, then switch the object off. Needs a transparent material.")]
    public bool fadesOut;

    [Header("Sound")]
    [Tooltip("Played once as the change starts (after the delay). Use a 3D source so it comes from this object.")]
    public AudioSource sound;

    private Renderer targetRenderer;
    private Material materialInstance;
    private Light targetLight;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startScale;
    private Color startBaseColor;
    private Color startEmissionColor;
    private Color startLightColor;
    private float startLightIntensity;

    private bool cached;
    private bool running;

    /// <summary>True once the change has finished playing.</summary>
    public bool hasPlayed { get; private set; }

    private void Awake()
    {
        Cache();
    }

    private void Cache()
    {
        if (cached)
        {
            return;
        }

        cached = true;

        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
        startScale = transform.localScale;

        if (recolors || fadesOut)
        {
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            if (targetRenderer != null)
            {
                materialInstance = targetRenderer.material;
                startBaseColor = materialInstance.HasProperty("_BaseColor")
                    ? materialInstance.GetColor("_BaseColor")
                    : Color.white;
                startEmissionColor = materialInstance.HasProperty("_EmissionColor")
                    ? materialInstance.GetColor("_EmissionColor")
                    : Color.black;
            }
        }

        if (relights)
        {
            targetLight = GetComponent<Light>();
            if (targetLight == null)
            {
                targetLight = GetComponentInChildren<Light>();
            }

            if (targetLight != null)
            {
                startLightColor = targetLight.color;
                startLightIntensity = targetLight.intensity;
            }
        }
    }

    /// <summary>Run the transition once. Safe to call from a UnityEvent.</summary>
    public void Play()
    {
        Cache();

        if (running || hasPlayed || !isActiveAndEnabled)
        {
            return;
        }

        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        running = true;

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (sound != null)
        {
            sound.Play();
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float linear = Mathf.Clamp01(elapsed / duration);
            Apply(ease.Evaluate(linear), linear);
            yield return null;
        }

        Apply(ease.Evaluate(1f), 1f);

        running = false;
        hasPlayed = true;

        if (fadesOut)
        {
            gameObject.SetActive(false);
        }
    }

    private void Apply(float eased, float linear)
    {
        if (movesLocally)
        {
            transform.localPosition = Vector3.LerpUnclamped(startPosition, targetLocalPosition, eased);
        }

        if (rotatesLocally)
        {
            transform.localRotation = Quaternion.SlerpUnclamped(startRotation, Quaternion.Euler(targetLocalEuler), eased);
        }

        if (scales)
        {
            // The pop is a half sine so it swells mid-change and settles exactly on target.
            float pop = 1f + (scalePop - 1f) * Mathf.Sin(linear * Mathf.PI);
            transform.localScale = Vector3.LerpUnclamped(startScale, targetLocalScale, eased) * pop;
        }

        if (materialInstance != null)
        {
            if (recolors)
            {
                materialInstance.SetColor("_BaseColor", Color.Lerp(startBaseColor, targetBaseColor, eased));
                materialInstance.EnableKeyword("_EMISSION");
                materialInstance.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                materialInstance.SetColor("_EmissionColor",
                    Color.Lerp(startEmissionColor, targetEmissionColor * targetEmissionIntensity, eased));
            }

            if (fadesOut)
            {
                Color faded = materialInstance.GetColor("_BaseColor");
                faded.a = Mathf.Lerp(startBaseColor.a, 0f, eased);
                materialInstance.SetColor("_BaseColor", faded);
            }
        }

        if (relights && targetLight != null)
        {
            targetLight.color = Color.Lerp(startLightColor, targetLightColor, eased);
            targetLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, eased);
        }
    }
}
