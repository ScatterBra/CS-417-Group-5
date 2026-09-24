using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Enter the desk clue's sequence to release the cabinet key.</summary>
public sealed class LabSequencePuzzle : MonoBehaviour
{
    [SerializeField] private string puzzleId = "labDeskSequence";
    [SerializeField] private int[] sequence = { 2, 1, 3 };
    [SerializeField] private Renderer[] progressLights;
    [SerializeField] private TextMesh statusText;
    [SerializeField] private Transform cabinetDoor;
    [SerializeField] private float openHeight = 0.68f;
    [SerializeField, Min(0.1f)] private float openDuration = 1.2f;
    [SerializeField] private XRGrabInteractable cabinetKey;
    [SerializeField] private Rigidbody keyBody;

    private int step;
    private bool solved;
    private float nextPressTime;
    private Vector3 closedPosition;
    private string ProgressId => gameObject.scene.path + "::" + puzzleId;

    private void Start()
    {
        closedPosition = cabinetDoor.localPosition;
        solved = GameProgress.IsPuzzleCompleted(ProgressId) ||
                 GameProgress.IsRoomCompleted(gameObject.scene.path);
        cabinetKey.enabled = false;
        keyBody.isKinematic = true;
        if (solved)
        {
            step = sequence.Length;
            cabinetDoor.localPosition = closedPosition + Vector3.up * openHeight;
            ReleaseKey();
        }
        RefreshLights();
        statusText.text = solved ? "ACCESS GRANTED" : "ENTER 3-STEP SEQUENCE";
    }

    // Wired to each prebuilt XRSimpleInteractable's Select Entered event.
    public void PressButton(int number)
    {
        if (solved || Time.time < nextPressTime) return;
        nextPressTime = Time.time + 0.2f;
        if (number != sequence[step])
        {
            step = 0;
            statusText.text = "WRONG ORDER - TRY AGAIN";
            RefreshLights();
            return;
        }
        step++;
        RefreshLights();
        statusText.text = $"SEQUENCE {step} / {sequence.Length}";
        if (step != sequence.Length) return;
        solved = true;
        GameProgress.CompletePuzzle(ProgressId);
        statusText.text = "ACCESS GRANTED";
        StartCoroutine(OpenCabinet());
    }

    private void RefreshLights()
    {
        for (int i = 0; i < progressLights.Length; i++)
            progressLights[i].material.color = i < step ? Color.green : new Color(0.12f, 0.18f, 0.22f);
    }

    private IEnumerator OpenCabinet()
    {
        Vector3 openPosition = closedPosition + Vector3.up * openHeight;
        for (float time = 0; time < openDuration; time += Time.deltaTime)
        {
            cabinetDoor.localPosition = Vector3.Lerp(closedPosition, openPosition,
                Mathf.SmoothStep(0, 1, time / openDuration));
            yield return null;
        }
        cabinetDoor.localPosition = openPosition;
        ReleaseKey();
    }

    private void ReleaseKey()
    {
        // The room controller secures keys permanently after all locks are solved.
        if (GameProgress.IsRoomCompleted(gameObject.scene.path)) return;
        cabinetKey.enabled = true;
        keyBody.isKinematic = false;
    }
}
