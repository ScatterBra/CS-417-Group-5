using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// A puzzle solved by doing things in a set order - pressing keys, pulling plugs, anything
/// that reports a label. The right order releases a Key; a wrong step starts it over.
/// With Check When Complete on it behaves like a code lock instead: every input is taken,
/// and only a full-length entry is checked, so the answer can't be found one step at a time.
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
        /// <summary>Taken but not judged yet (code-lock mode), or the entry was cleared.</summary>
        Entered,
    }

    [Tooltip("Unique within the scene. Used to remember the puzzle is solved for this play session.")]
    public string puzzleId = "officePuzzle";

    [Tooltip("Labels in the order they must be entered. Matches each input's Label (not case sensitive).")]
    public string[] sequence = { "B", "O", "S", "S"};

    [Tooltip("Code-lock mode: accept any input and only check once the entry is as long as the " +
             "sequence. Off = every step is checked as it is entered.")]
    public bool checkWhenComplete;

    [Tooltip("Inputs that wipe the current entry instead of being entered, e.g. C and AC.")]
    public string[] clearLabels = Array.Empty<string>();

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

    [Tooltip("Grow the released Key in with an eased pop instead of just appearing.")]
    public bool popInReleasedKey = true;

    [Min(0.05f)]
    public float popInSeconds = 0.6f;

    [Tooltip("Played as the released Key appears. Put it on the Key so the sound comes from where it pops in.")]
    public AudioSource releaseSound;

    public EasedStateChange[] solvedStateChanges = Array.Empty<EasedStateChange>();

    public UnityEvent onSolved = new UnityEvent();
    public UnityEvent onWrong = new UnityEvent();

    /// <summary>Raised on every wrong step. Inputs that hold state (plugs) put themselves back.</summary>
    public event Action wrongStep;

    /// <summary>How many steps are done.</summary>
    public int progress { get; private set; }

    public bool isSolved { get; private set; }

    private Coroutine flash;
    private readonly List<string> entered = new List<string>();

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

        if (Array.Exists(clearLabels, c => string.Equals(c, label, StringComparison.OrdinalIgnoreCase)))
        {
            progress = 0;
            entered.Clear();
            Refresh();
            return Result.Entered;
        }

        if (checkWhenComplete)
        {
            return SubmitToCode(label);
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

        return Fail();
    }

    private Result SubmitToCode(string label)
    {
        entered.Add(label);
        progress = entered.Count;
        if (entered.Count < sequence.Length)
        {
            Refresh();
            return Result.Entered;
        }

        for (int i = 0; i < sequence.Length; i++)
        {
            if (!string.Equals(entered[i], sequence[i], StringComparison.OrdinalIgnoreCase))
            {
                return Fail();
            }
        }

        Solve();
        return Result.Solved;
    }

    private Result Fail()
    {
        progress = 0;
        entered.Clear();
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
            if (popInReleasedKey && isActiveAndEnabled)
            {
                StartCoroutine(PopIn(releasedKey.transform));
            }

            if (releaseSound != null)
            {
                releaseSound.Play();
            }
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

    /// <summary>Grows the Key from nothing with a slight overshoot, held still by physics until it's full size.</summary>
    private IEnumerator PopIn(Transform key)
    {
        Vector3 fullScale = key.localScale;
        Rigidbody[] bodies = key.GetComponentsInChildren<Rigidbody>();
        bool[] wasKinematic = new bool[bodies.Length];
        for (int i = 0; i < bodies.Length; i++)
        {
            wasKinematic[i] = bodies[i].isKinematic;
            bodies[i].isKinematic = true;
        }

        for (float time = 0f; time < popInSeconds; time += Time.deltaTime)
        {
            // Ease-out-back: fast out of nothing, a little past full size, then settle.
            float x = time / popInSeconds - 1f;
            float s = 1f + 2.70158f * x * x * x + 1.70158f * x * x;
            key.localScale = fullScale * Mathf.Max(0.001f, s);
            yield return null;
        }

        key.localScale = fullScale;

        // A hand may already have it; the grab then owns the body's physics.
        XRBaseInteractable grab = key.GetComponentInChildren<XRBaseInteractable>();
        if (grab != null && grab.isSelected)
        {
            yield break;
        }

        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i] != null)
            {
                bodies[i].isKinematic = wasKinematic[i];
            }
        }
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
            if (i >= progress)
            {
                text.Append(emptySlot);
                continue;
            }

            // In code-lock mode show what was typed, never the answer.
            string done = checkWhenComplete ? entered[i] : sequence[i];
            text.Append(showEnteredLabels ? done : "•");
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
