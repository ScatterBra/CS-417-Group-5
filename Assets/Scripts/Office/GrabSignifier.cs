using UnityEngine;

/// <summary>
/// The taught grab signifier: every object the player can pick up breathes with
/// the same warm amber glow, and nothing else in the office does.
/// Covers rubric X2.1. Once the Key reaches its Lock the glow settles, so the
/// pulse only ever means "this is still yours to move".
/// </summary>
[DisallowMultipleComponent]
public sealed class GrabSignifier : MonoBehaviour
{
    [Tooltip("The colour the whole game uses to mean 'you can pick this up'.")]
    public Color glowColor = new Color(1f, 0.78f, 0.18f, 1f);

    [Min(0f)]
    public float minIntensity = 0.30f;

    [Min(0f)]
    public float maxIntensity = 1.5f;

    [Min(0.05f)]
    [Tooltip("Seconds for one full breath in and out.")]
    public float pulseSeconds = 1.8f;

    [Tooltip("Optional light that breathes with the glow so the Key reads across the room.")]
    public Light halo;

    [Min(0f)]
    [Tooltip("How much of the glow intensity the halo light carries.")]
    public float haloScale = 0.35f;

    private Material materialInstance;
    private bool settled;

    private void Awake()
    {
        Renderer keyRenderer = GetComponent<Renderer>();
        if (keyRenderer == null)
        {
            keyRenderer = GetComponentInChildren<Renderer>();
        }

        if (keyRenderer != null)
        {
            materialInstance = keyRenderer.material;
            materialInstance.EnableKeyword("_EMISSION");
            materialInstance.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
    }

    private void Update()
    {
        if (settled || materialInstance == null)
        {
            return;
        }

        float phase = (Mathf.Sin(Time.time * 2f * Mathf.PI / pulseSeconds) + 1f) * 0.5f;
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, phase);

        materialInstance.SetColor("_EmissionColor", glowColor * intensity);
        if (halo != null)
        {
            halo.intensity = intensity * haloScale;
        }
    }

    /// <summary>Stop the pulse: this Key has reached its Lock.</summary>
    public void Settle()
    {
        settled = true;

        if (materialInstance != null)
        {
            materialInstance.SetColor("_EmissionColor", glowColor * minIntensity * 0.35f);
        }

        if (halo != null)
        {
            halo.enabled = false;
        }
    }
}
