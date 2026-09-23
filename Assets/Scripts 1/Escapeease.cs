using System.Collections;
using UnityEngine;

/// REVERSIBLE eased mover for things that go back and forth: puzzle levers, press-buttons.
/// (Lock signifiers and win effects use the team's one-shot EasedStateChange instead.)
/// Call Play() to go to the "changed" state and Reverse() to go back.
/// NOTE: keep this component on an always-active object. If Target is the same object and
/// "disableTargetWhenDone" is on, you won't be able to Reverse().
public class EscapeEase : MonoBehaviour
{
    [Header("What changes")]
    public Transform target;                         // defaults to this transform
    public Vector3 moveOffset = Vector3.zero;        // local space
    public Vector3 rotateOffset = Vector3.zero;      // euler degrees
    public Vector3 scaleMultiplier = Vector3.one;

    [Header("Look")]
    public Renderer colorRenderer;
    public bool changeColor;
    public Color toColor = Color.green;
    public bool changeEmission;
    [ColorUsage(false, true)] public Color toEmission = Color.yellow;
    public Light lightToFade;
    public float lightToIntensity = 2f;

    [Header("Timing")]
    public float duration = 1.5f;
    public bool disableTargetWhenDone;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    Vector3 startPos, startScale;
    Quaternion startRot;
    Material mat;
    Color startColor, startEmission;
    float startLight;
    float progress;
    Coroutine routine;

    void Awake()
    {
        if (!target) target = transform;
        startPos = target.localPosition;
        startRot = target.localRotation;
        startScale = target.localScale;

        if (colorRenderer && (changeColor || changeEmission))
        {
            mat = colorRenderer.material;
            startColor = mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId)
                       : mat.HasProperty(ColorId) ? mat.GetColor(ColorId) : Color.white;
            startEmission = mat.HasProperty(EmissionId) ? mat.GetColor(EmissionId) : Color.black;
            if (changeEmission) mat.EnableKeyword("_EMISSION");
        }
        if (lightToFade) startLight = lightToFade.intensity;
    }

    public void Play() => Go(1f);
    public void Reverse() => Go(0f);

    void Go(float goal)
    {
        if (target && disableTargetWhenDone) target.gameObject.SetActive(true);
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run(goal));
    }

    IEnumerator Run(float goal)
    {
        float from = progress;
        float dur = Mathf.Max(0.01f, duration * Mathf.Abs(goal - from));
        for (float t = 0f; t < 1f; t += Time.deltaTime / dur)
        {
            progress = Mathf.Lerp(from, goal, t);
            Apply(Mathf.SmoothStep(0f, 1f, progress));   // ease in AND out
            yield return null;
        }
        progress = goal;
        Apply(goal);
        if (goal >= 1f && disableTargetWhenDone && target) target.gameObject.SetActive(false);
    }

    void Apply(float e)
    {
        target.localPosition = startPos + moveOffset * e;
        target.localRotation = startRot * Quaternion.Euler(rotateOffset * e);
        target.localScale = Vector3.Lerp(startScale, Vector3.Scale(startScale, scaleMultiplier), e);

        if (mat)
        {
            if (changeColor)
            {
                Color c = Color.Lerp(startColor, toColor, e);
                if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, c);
                if (mat.HasProperty(ColorId)) mat.SetColor(ColorId, c);
            }
            if (changeEmission) mat.SetColor(EmissionId, Color.Lerp(startEmission, toEmission, e));
        }
        if (lightToFade) lightToFade.intensity = Mathf.Lerp(startLight, lightToIntensity, e);
    }
}
