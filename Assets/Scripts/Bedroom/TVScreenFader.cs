using UnityEngine;
using System.Collections;

public class TVScreenFader : MonoBehaviour
{
    public Renderer screenRenderer;
    public float fadeDuration = 1.5f;

    [Tooltip("The glowing Brat green color when fully on.")]
    [ColorUsage(false, true)] // This enables the HDR color picker in the Inspector
    public Color targetEmissionColor = Color.green;

    private void Start()
    {
        // Force the screen to be completely invisible and unlit on startup
        if (screenRenderer != null)
        {
            Material mat = screenRenderer.material;
            
            Color color = mat.color;
            color.a = 0f;
            mat.color = color;
            
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    public void FadeScreenIn() 
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1f)); // 1 = fully visible
    }

    public void FadeScreenOut() 
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(0f)); // 0 = fully transparent
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (screenRenderer == null) yield break;

        Material mat = screenRenderer.material;
        Color color = mat.color;
        float startAlpha = color.a;
        
        Color startEmission = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
        Color endEmission = (targetAlpha > 0.5f) ? targetEmissionColor : Color.black;

        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;
            
            // Fade Alpha
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            mat.color = color;
            
            // Fade Emission
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", Color.Lerp(startEmission, endEmission, t));
            }
            
            yield return null;
        }

        // Snap to exact target values at the end
        color.a = targetAlpha;
        mat.color = color;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", endEmission);
        }
    }
}