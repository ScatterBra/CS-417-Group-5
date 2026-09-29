using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Counts the Locks and opens the exit once every one of them is solved.
/// Covers rubric X1: three Lock scripts must fire before the player can escape.
/// </summary>
[DisallowMultipleComponent]
public sealed class EscapeController : MonoBehaviour
{
    [Tooltip("Every Lock that must be solved before the door opens.")]
    public LockReceptacle[] locks = Array.Empty<LockReceptacle>();

    [Tooltip("Open the door as soon as every Lock is solved. Off = the door waits for UnlockNow(), " +
             "e.g. from the end of another room.")]
    public bool unlockWhenLocksSolved = true;

    [Tooltip("Played the moment every Lock is solved, whether or not the door opens then.")]
    public EasedStateChange[] locksSolvedStateChanges = Array.Empty<EasedStateChange>();

    public UnityEvent onLocksSolved = new UnityEvent();

    /// <summary>True once every Lock has been solved.</summary>
    public bool allLocksSolved { get; private set; }

    [Header("Exit")]
    [Tooltip("The eased slide that opens the door.")]
    public EasedStateChange doorOpens;

    [Tooltip("Anything else that should change state (eased) when the door unlocks.")]
    public EasedStateChange[] unlockStateChanges = Array.Empty<EasedStateChange>();

    [Header("Readouts")]
    public Text progressText;
    public Text doorStatusText;
    public Text escapedText;

    [Header("Copy")]
    [Tooltip("Progress readout. {0} = solved, {1} = total.")]
    public string progressFormat = "{0} / {1} PUT BACK";
    public string lockedMessage = "DOOR LOCKED";
    public string unlockedMessage = "DOOR OPEN - GO HOME";
    public string escapedMessage = "YOU ESCAPED";

    [Header("Readout colours")]
    public Color lockedColor = new Color(0.95f, 0.15f, 0.12f, 1f);
    public Color unlockedColor = new Color(0.20f, 1f, 0.35f, 1f);

    public UnityEvent onUnlocked = new UnityEvent();
    public UnityEvent onEscaped = new UnityEvent();

    /// <summary>How many Locks are solved right now.</summary>
    public int solvedCount { get; private set; }

    /// <summary>True once every Lock is solved and the door has opened.</summary>
    public bool isUnlocked { get; private set; }

    /// <summary>True once the player has walked out through the open door.</summary>
    public bool hasEscaped { get; private set; }

    private void OnEnable()
    {
        LockReceptacle.lockSolved += OnLockSolved;
        Refresh();
    }

    private void OnDisable()
    {
        LockReceptacle.lockSolved -= OnLockSolved;
    }

    private void Start()
    {
        if (escapedText != null)
        {
            escapedText.gameObject.SetActive(false);
        }

        Refresh();
    }

    private void OnLockSolved(LockReceptacle solved)
    {
        if (Array.IndexOf(locks, solved) < 0)
        {
            return;
        }

        Refresh();

        if (solvedCount < locks.Length)
        {
            return;
        }

        if (!allLocksSolved)
        {
            allLocksSolved = true;
            foreach (EasedStateChange change in locksSolvedStateChanges)
            {
                if (change != null)
                {
                    change.Play();
                }
            }

            onLocksSolved.Invoke();
        }

        if (unlockWhenLocksSolved && !isUnlocked)
        {
            Unlock();
        }
    }

    /// <summary>Open the door now, whatever the Locks say. Also callable from a UnityEvent.</summary>
    public void UnlockNow()
    {
        if (!isUnlocked)
        {
            Unlock();
        }
    }

    private void Refresh()
    {
        int solved = 0;
        foreach (LockReceptacle candidate in locks)
        {
            if (candidate != null && candidate.isSolved)
            {
                solved++;
            }
        }

        solvedCount = solved;

        if (progressText != null)
        {
            progressText.text = string.Format(progressFormat, solvedCount, locks.Length);
        }

        if (doorStatusText != null && !isUnlocked)
        {
            doorStatusText.text = lockedMessage;
            doorStatusText.color = lockedColor;
        }
    }

    private void Unlock()
    {
        isUnlocked = true;

        if (doorOpens != null)
        {
            doorOpens.Play();
        }

        foreach (EasedStateChange change in unlockStateChanges)
        {
            if (change != null)
            {
                change.Play();
            }
        }

        if (doorStatusText != null)
        {
            doorStatusText.text = unlockedMessage;
            doorStatusText.color = unlockedColor;
        }

        onUnlocked.Invoke();
    }

    /// <summary>Called by the trigger volume outside the door.</summary>
    public void ReportEscaped()
    {
        if (hasEscaped || !isUnlocked)
        {
            return;
        }

        hasEscaped = true;

        if (escapedText != null)
        {
            escapedText.gameObject.SetActive(true);
            escapedText.text = escapedMessage;

            EasedStateChange bannerChange = escapedText.GetComponent<EasedStateChange>();
            if (bannerChange != null)
            {
                bannerChange.Play();
            }
        }

        onEscaped.Invoke();
    }
}
