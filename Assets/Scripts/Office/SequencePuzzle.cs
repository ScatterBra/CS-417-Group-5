using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// A puzzle solved by doing things in a set order - pressing keys, pulling plugs, anything
/// that reports a label. The right order releases a Key; a wrong step starts it over.
/// Covers the Puzzle System side quest, and each instance is one entry of Puzzle Content.
/// </summary>
[DisallowMultipleComponent]
public sealed class SequencePuzzle : MonoBehaviour
{
    public enum Result
    {
        Ignored,
        Accepted,
        Wrong,
        Solved,
    }

    [Tooltip("Unique within the scene. Used to remember the puzzle is solved for this play session.")]
    public string puzzleId = "officePuzzle";

    [Tooltip("Labels in the order they must be entered. Matches each input's Label (not case sensitive).")]
    public string[] sequence = { "H", "O", "M", "E" };

    [Header("Readout (optional)")]
    [Tooltip("Shows progress, e.g. text on a screen.")]
    public Text display;
    public string emptySlot = "_";
    public string solvedMessage = "UNLOCKED";
    public string wrongMessage = "WRONG";
    [Tooltip("Show the entered labels. Off = show a dot per step, so the answer isn't spelled out.")]
    public bool showEnteredLabels = true;

    [Header("Reward")]
    [Tooltip("Hidden until the puzzle is solved, then appears where it was placed: the released Key.")]
    public GameObject releasedKey;
    public EasedStateChange[] solvedStateChanges = Array.Empty<EasedStateChange>();

    public UnityEvent onSolved = new UnityEvent();
    public UnityEvent onWrong = new UnityEvent();

    /// <summary>Raised on every wrong step. Inputs that hold state (plugs) put themselves back.</summary>
    public event Action wrongStep;

    /// <summary>How many steps are done.</summary>
    public int progress { get; private set; }

    public bool isSolved { get; private set; }

    private Coroutine flash;

    private string ProgressId => gameObject.scene.path + "::" + puzzleId;

    private void Start()
    {
        if (GameProgress.IsPuzzleCompleted(ProgressId))
        {
            // Coming back to the room during the same session: stay solved.
            isSolved = true;
            progress = sequence.Length;
        }
        else if (releasedKey != null)
        {
            releasedKey.SetActive(false);
        }

        Refresh();
    }

    /// <summary>Called by an input when the player operates it.</summary>
    public Result Submit(string label)
    {
        if (isSolved || sequence.Length == 0)
        {
            return Result.Ignored;
        }

        if (string.Equals(label, sequence[progress], StringComparison.OrdinalIgnoreCase))
        {
            progress++;
            if (progress < sequence.Length)
            {
                Refresh();
                return Result.Accepted;
            }

            Solve();
            return Result.Solved;
        }

        progress = 0;
        ShowFor(wrongMessage, 0.8f);
        onWrong.Invoke();
        wrongStep?.Invoke();
        return Result.Wrong;
    }

    /// <summary>Solve it outright. Also callable from a UnityEvent while testing.</summary>
    public void Solve()
    {
        if (isSolved)
        {
            return;
        }

        isSolved = true;
        progress = sequence.Length;
        GameProgress.CompletePuzzle(ProgressId);

        if (releasedKey != null)
        {
            releasedKey.SetActive(true);
        }

        foreach (EasedStateChange change in solvedStateChanges)
        {
            if (change != null)
            {
                change.Play();
            }
        }

        Refresh();
        onSolved.Invoke();
    }

    private void Refresh()
    {
        if (display == null || flash != null)
        {
            return;
        }

        if (isSolved)
        {
            display.text = solvedMessage;
            return;
        }

        var text = new StringBuilder();
        for (int i = 0; i < sequence.Length; i++)
        {
            if (i > 0) text.Append(' ');
            text.Append(i < progress ? (showEnteredLabels ? sequence[i] : "•") : emptySlot);
        }

        display.text = text.ToString();
    }

    private void ShowFor(string message, float seconds)
    {
        if (display == null || !isActiveAndEnabled)
        {
            return;
        }

        if (flash != null)
        {
            StopCoroutine(flash);
        }

        flash = StartCoroutine(Flash(message, seconds));
    }

    private IEnumerator Flash(string message, float seconds)
    {
        display.text = message;
        yield return new WaitForSeconds(seconds);
        flash = null;
        Refresh();
    }
}
