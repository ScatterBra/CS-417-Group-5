using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Opens the lab door when the three matching keys are inserted.</summary>
public sealed class LabEscapeRoom : MonoBehaviour
{
    [SerializeField] private LabKeySocket[] sockets = new LabKeySocket[3];
    [SerializeField] private Renderer[] indicators = new Renderer[3];
    [SerializeField] private TextMesh progressText;
    [SerializeField] private Transform doorPanel;
    [SerializeField, Min(0.1f)] private float doorOpenHeight = 3f;

    private readonly bool[] unlocked = new bool[3];
    private int unlockedCount;

    private void Awake()
    {
        if (sockets.Length != 3 || indicators.Length != 3 ||
            progressText == null || doorPanel == null)
        {
            Debug.LogError("Assign the three lab sockets, indicators, progress text, and door.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < sockets.Length; i++)
        {
            if (sockets[i] == null || indicators[i] == null)
            {
                Debug.LogError($"Lab socket {i + 1} is missing a scene reference.", this);
                enabled = false;
                return;
            }

            int id = i;
            sockets[i].selectEntered.AddListener(args => OnKeyInserted(id, args));
        }

        UpdateProgress();
    }

    private void OnKeyInserted(int id, SelectEnterEventArgs args)
    {
        if (unlocked[id]) return;
        LabKeyToken key = args.interactableObject.transform.GetComponentInParent<LabKeyToken>();
        if (key == null || key.Id != id) return;

        unlocked[id] = true;
        unlockedCount++;
        indicators[id].material.color = Color.green;
        StartCoroutine(SecureKey(key, sockets[id].transform));
        UpdateProgress();
        if (unlockedCount == sockets.Length) StartCoroutine(OpenDoor());
    }

    private void UpdateProgress()
    {
        progressText.text = $"EXIT LOCKS  {unlockedCount} / {sockets.Length}";
    }

    private static IEnumerator SecureKey(LabKeyToken key, Transform socket)
    {
        yield return null; // Let XR Interaction Toolkit finish selecting the socket.
        if (key == null) yield break;
        key.GetComponent<XRGrabInteractable>().enabled = false;
        Rigidbody body = key.GetComponent<Rigidbody>();
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        key.transform.SetPositionAndRotation(socket.position + socket.forward * 0.13f, socket.rotation);
    }

    private IEnumerator OpenDoor()
    {
        Vector3 closed = doorPanel.localPosition;
        Vector3 open = closed + Vector3.up * doorOpenHeight;
        for (float elapsed = 0f; elapsed < 2f; elapsed += Time.deltaTime)
        {
            doorPanel.localPosition = Vector3.Lerp(closed, open,
                Mathf.SmoothStep(0f, 1f, elapsed / 2f));
            yield return null;
        }
        doorPanel.localPosition = open;
        progressText.text = "ESCAPE OPEN";
        progressText.color = Color.green;
    }
}
