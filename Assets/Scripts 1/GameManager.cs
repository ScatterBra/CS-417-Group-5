using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// One per escape-room scene. Tracks keys found, locks solved, collectibles, and triggers the win.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Rules")]
    public int locksRequired = 3;

    [Header("Scoreboards (World Space TMP text)")]
    public TMP_Text progressText;              // Progress Scoreboard
    public TMP_Text collectiblesText;          // Collectibles scoreboard

    [Header("Win Celebration (pick ONE style)")]
    public float winDelay = 2f;                // lets the last lock's ease finish first
    public string winSceneName = "";           // style A: load a "You Win" scene
    public EasedStateChange[] winChanges;      // style B: change 2+ objects...
    public GameObject winMessage;              // ...and show a "You Win" text object
    public UnityEvent onWin;

    readonly HashSet<string> discoveredKeys = new HashSet<string>();
    int solved, collected, collectibleTotal;
    bool won;

    void Awake() { Instance = this; }

    void Start()
    {
        collectibleTotal = Collectible.All.Count;
        if (winMessage) winMessage.SetActive(false);
        RefreshUI();
    }

    public void KeyDiscovered(string id)
    {
        if (discoveredKeys.Add(id)) RefreshUI();
    }

    public void AddCollectible(int amount = 1)
    {
        collected += amount;
        RefreshUI();
    }

    public void LockSolved(Lock l)
    {
        solved++;
        RefreshUI();
        if (solved >= locksRequired && !won) StartCoroutine(WinRoutine());
    }

    IEnumerator WinRoutine()
    {
        won = true;
        yield return new WaitForSeconds(winDelay);
        if (!string.IsNullOrEmpty(winSceneName))
        {
            SceneManager.LoadScene(winSceneName);
            yield break;
        }
        if (winMessage) winMessage.SetActive(true);
        foreach (var c in winChanges) if (c) c.Play();
        onWin.Invoke();
    }

    void RefreshUI()
    {
        if (progressText)
            progressText.text =
                $"Keys left to find: {Mathf.Max(0, locksRequired - discoveredKeys.Count)}\n" +
                $"Locks left to solve: {Mathf.Max(0, locksRequired - solved)}";
        if (collectiblesText)
            collectiblesText.text = $"Collected: {collected}/{collectibleTotal}";
    }

    // Hook to a Restart button.
    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}
