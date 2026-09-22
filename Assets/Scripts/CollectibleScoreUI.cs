using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates a small world-space score display in front of the active player camera.
/// This works in the desktop preview and follows the headset camera in VR.
/// </summary>
[DisallowMultipleComponent]
public sealed class CollectibleScoreUI : MonoBehaviour
{
    private Text scoreText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureScoreUIExists()
    {
        if (FindFirstObjectByType<CollectibleScoreUI>() != null)
        {
            return;
        }

        new GameObject(
            "Collectible Score UI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(CollectibleScoreUI));
    }

    private void Awake()
    {
        BuildUI();
        AttachToPlayerCamera();
        RefreshScore(CollectibleItem.TotalScore);
    }

    private void OnEnable()
    {
        CollectibleItem.ScoreChanged += RefreshScore;
    }

    private void OnDisable()
    {
        CollectibleItem.ScoreChanged -= RefreshScore;
    }

    private void LateUpdate()
    {
        if (transform.parent == null)
        {
            AttachToPlayerCamera();
        }
    }

    private void AttachToPlayerCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        transform.SetParent(camera.transform, false);
        transform.localPosition = new Vector3(-0.5f, 0.32f, 1.25f);
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one * 0.0015f;

        Canvas canvas = GetComponent<Canvas>();
        canvas.worldCamera = camera;
    }

    private void BuildUI()
    {
        RectTransform rootRect = (RectTransform)transform;
        rootRect.sizeDelta = new Vector2(420f, 100f);

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 3f;

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(transform, false);
        RectTransform backgroundRect = (RectTransform)background.transform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0.02f, 0.08f, 0.13f, 0.88f);

        GameObject label = new GameObject("Score", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(background.transform, false);
        RectTransform labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(18f, 8f);
        labelRect.offsetMax = new Vector2(-18f, -8f);

        scoreText = label.GetComponent<Text>();
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.fontSize = 46;
        scoreText.fontStyle = FontStyle.Bold;
        scoreText.alignment = TextAnchor.MiddleCenter;
        scoreText.color = new Color(0.75f, 1f, 1f, 1f);
    }

    private void RefreshScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"SCORE  {score}";
        }
    }
}
