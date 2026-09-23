using UnityEngine;
using UnityEngine.Events;

/// Puzzle System: a specific order of PuzzleInputs (levers/buttons) releases a Key.
/// Put on an empty parent object. Each PuzzleInput has an ID; list the correct order of IDs.
/// Solving reveals a hidden key (start it INACTIVE) and/or Instantiates a key prefab.
public class PuzzleSequence : MonoBehaviour
{
    public int[] correctOrder = { 1, 3, 2 };
    public PuzzleInput[] inputs;

    [Header("Reward")]
    public GameObject keyToReveal;             // inactive key that becomes active
    public GameObject keyPrefab;               // OR a prefab (with GrabbableProp) to spawn
    public Transform spawnPoint;

    public UnityEvent onSolved;
    public UnityEvent onFailed;

    int step;
    bool solved;

    public void Press(int id)
    {
        if (solved || correctOrder.Length == 0) return;

        if (id == correctOrder[step])
        {
            step++;
            if (step >= correctOrder.Length) Solve();
        }
        else Fail();
    }

    void Solve()
    {
        solved = true;
        if (keyToReveal) keyToReveal.SetActive(true);
        if (keyPrefab)
        {
            Vector3 p = spawnPoint ? spawnPoint.position : transform.position;
            Quaternion r = spawnPoint ? spawnPoint.rotation : Quaternion.identity;
            Instantiate(keyPrefab, p, r);
        }
        onSolved.Invoke();
    }

    void Fail()
    {
        step = 0;
        foreach (var i in inputs) if (i) i.ResetVisual();
        onFailed.Invoke();
    }
}
