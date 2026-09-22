using UnityEngine;
using UnityEngine.UI;

/// <summary>Updates the Text assigned in the Inspector on a scene-authored Canvas.</summary>
[DisallowMultipleComponent]
public sealed class CollectibleScoreUI : MonoBehaviour
{
    [Tooltip("Drag the Canvas's score Text component here.")]
    public Text scoreText;

    private void OnEnable()
    {
        CollectibleItem.ScoreChanged += RefreshScore;
        RefreshScore(CollectibleItem.TotalScore);
    }

    private void Start()
    {
        RefreshScore(CollectibleItem.TotalScore);
    }

    private void OnDisable()
    {
        CollectibleItem.ScoreChanged -= RefreshScore;
    }

    private void RefreshScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"SCORE  {score}";
    }
}
