using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// Put on each piece of hidden writing (a TextMeshPro object, or a quad with an image).
/// Normally invisible. A BlacklightProp reveals it when pointed at it within range/angle.
public class InvisibleWriting : MonoBehaviour
{
    public static readonly List<InvisibleWriting> All = new List<InvisibleWriting>();

    [Tooltip("The hidden text/graphic. Leave empty to use this object's own component instead.")]
    public GameObject hiddenContent;

    [Tooltip("How fast it fades in/out per second.")]
    public float fadeSpeed = 4f;

    TMP_Text tmp;                 // used if the target is TextMeshPro (3D or UI)
    Renderer rend;                // used if the target is a plain quad/material instead
    Material mat;
    Color shownColor, hiddenColor;
    bool isRevealed;
    float amount; // 0 = hidden, 1 = revealed

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake()
    {
        var target = hiddenContent ? hiddenContent : gameObject;

        tmp = target.GetComponent<TMP_Text>();
        if (tmp == null) tmp = target.GetComponentInChildren<TMP_Text>();

        if (tmp != null)
        {
            shownColor = tmp.color;
            hiddenColor = shownColor; hiddenColor.a = 0f;
            tmp.color = hiddenColor;
        }
        else
        {
            rend = target.GetComponent<Renderer>();
            if (rend == null) rend = target.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                mat = rend.material;
                hiddenColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.black;
                hiddenColor.a = 0f;
                shownColor = hiddenColor; shownColor.a = 1f;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hiddenColor);
            }
        }

        if (hiddenContent) hiddenContent.SetActive(true); // stays active, we fade alpha instead
    }

    /// Called every frame by any BlacklightProp currently aimed at this object.
    public void Illuminate() { isRevealed = true; }

    void Update()
    {
        float target = isRevealed ? 1f : 0f;
        amount = Mathf.MoveTowards(amount, target, fadeSpeed * Time.deltaTime);

        if (tmp != null)
        {
            Color c = Color.Lerp(hiddenColor, shownColor, amount);
            tmp.color = c;
        }
        else if (mat != null && mat.HasProperty("_BaseColor"))
        {
            Color c = Color.Lerp(hiddenColor, shownColor, amount);
            mat.SetColor("_BaseColor", c);
        }

        isRevealed = false; // BlacklightProp must re-illuminate every frame it's aimed here
    }
}
