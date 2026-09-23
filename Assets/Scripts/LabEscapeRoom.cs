using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>Builds the three physical key-and-socket locks in the laboratory.</summary>
public sealed class LabEscapeRoom : MonoBehaviour
{
    [SerializeField] private Transform[] keySpawns = new Transform[3];
    [SerializeField] private Transform[] lockSlots = new Transform[3];
    [SerializeField] private GameObject[] keyModels = new GameObject[3];
    [SerializeField] private Transform doorPanel;
    [SerializeField, Min(0.1f)] private float keyLength = 0.28f;
    [SerializeField, Min(0.1f)] private float doorOpenHeight = 3f;

    private readonly Color[] keyColors =
    {
        new Color(0.95f, 0.15f, 0.12f),
        new Color(0.12f, 0.55f, 1f),
        new Color(1f, 0.76f, 0.12f),
    };

    private readonly bool[] unlocked = new bool[3];
    private readonly Renderer[] indicators = new Renderer[3];
    private TextMesh progressText;
    private int unlockedCount;

    private void Awake()
    {
        if (doorPanel == null || keySpawns.Length != 3 || lockSlots.Length != 3 ||
            keyModels.Length != 3)
        {
            Debug.LogError("Lab Escape Room needs three keys, three locks, and the door panel.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            if (keySpawns[i] == null || lockSlots[i] == null || keyModels[i] == null)
            {
                Debug.LogError($"Lab lock {i + 1} is missing a scene reference.", this);
                enabled = false;
                return;
            }
        }

        AddDirectInteractorsToHands();

        for (int i = 0; i < 3; i++)
        {
            CreateKey(i);
            CreateSocket(i);
        }

        CreateProgressDisplay();
        UpdateProgressDisplay();
    }

    private void CreateKey(int id)
    {
        Transform spawn = keySpawns[id];
        GameObject key = Instantiate(keyModels[id], spawn.position, spawn.rotation);
        key.name = $"Access Key {id + 1}";

        Renderer[] renderers = key.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError($"Access Key {id + 1} has no visible model.", key);
            Destroy(key);
            return;
        }

        Bounds bounds = GetRenderBounds(renderers);
        key.transform.localScale *= keyLength / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        bounds = GetRenderBounds(renderers);

        // A single collider and Rigidbody on the root make the entire model one grabbable prop.
        foreach (Collider oldCollider in key.GetComponentsInChildren<Collider>())
            oldCollider.enabled = false;

        BoxCollider collider = key.AddComponent<BoxCollider>();
        collider.center = key.transform.InverseTransformPoint(bounds.center);
        Vector3 scale = key.transform.lossyScale;
        collider.size = new Vector3(
            bounds.size.x / Mathf.Abs(scale.x),
            bounds.size.y / Mathf.Abs(scale.y),
            bounds.size.z / Mathf.Abs(scale.z));
        key.transform.position += Vector3.up * (bounds.extents.y + 0.12f);

        Rigidbody body = key.AddComponent<Rigidbody>();
        body.mass = 0.25f;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        key.AddComponent<XRGrabInteractable>().useDynamicAttach = true;
        key.AddComponent<LabKeyToken>().Id = id;
    }

    private void CreateSocket(int id)
    {
        GameObject socketObject = new GameObject($"Access Socket {id + 1}");
        socketObject.transform.SetPositionAndRotation(lockSlots[id].position, lockSlots[id].rotation);
        socketObject.transform.SetParent(lockSlots[id], true);

        BoxCollider trigger = socketObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0f, 0.2f);
        trigger.size = new Vector3(0.45f, 0.45f, 0.55f);

        LabKeySocket socket = socketObject.AddComponent<LabKeySocket>();
        socket.RequiredId = id;
        socket.showInteractableHoverMeshes = false;
        int slotId = id;
        socket.selectEntered.AddListener(args => OnKeyInserted(slotId, socket, args));

        CreateColoredCube("Socket plate", socketObject.transform,
            new Vector3(0f, 0f, -0.035f), new Vector3(0.48f, 0.48f, 0.08f),
            new Color(0.07f, 0.11f, 0.17f));
        indicators[id] = CreateColoredCube("Key indicator", socketObject.transform,
            new Vector3(0f, 0.13f, 0.035f), new Vector3(0.12f, 0.12f, 0.04f),
            keyColors[id]).GetComponent<Renderer>();
        CreateText(socketObject.transform, $"{new[] { "RED", "BLUE", "YELLOW" }[id]} ACCESS",
            new Vector3(0f, -0.39f, 0.13f), 0.012f, keyColors[id]);
    }

    private void OnKeyInserted(int id, LabKeySocket socket, SelectEnterEventArgs args)
    {
        if (unlocked[id])
            return;

        LabKeyToken key = args.interactableObject.transform.GetComponentInParent<LabKeyToken>();
        if (key == null || key.Id != id)
            return;

        unlocked[id] = true;
        unlockedCount++;
        StartCoroutine(SecureKey(key, socket.transform));
        StartCoroutine(AnimateIndicator(indicators[id], keyColors[id], Color.green));
        UpdateProgressDisplay();

        if (unlockedCount == 3)
            StartCoroutine(OpenDoor());
    }

    private static IEnumerator SecureKey(LabKeyToken key, Transform socket)
    {
        // Let XRI finish the selection event before fixing the key in place.
        yield return null;
        if (key == null)
            yield break;

        XRGrabInteractable grab = key.GetComponent<XRGrabInteractable>();
        if (grab != null)
            grab.enabled = false;

        Rigidbody body = key.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }
        key.transform.SetPositionAndRotation(socket.position + socket.forward * 0.13f, socket.rotation);
    }

    private static IEnumerator AnimateIndicator(Renderer indicator, Color from, Color to)
    {
        if (indicator == null)
            yield break;

        Material material = indicator.material;
        for (float elapsed = 0f; elapsed < 0.7f; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.7f);
            material.color = Color.Lerp(from, to, t);
            yield return null;
        }
        material.color = to;
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

    private void CreateProgressDisplay()
    {
        progressText = CreateText(transform, "", new Vector3(0f, 3.05f, -4.47f),
            0.025f, Color.white);
        progressText.name = "Door lock progress";
    }

    private void UpdateProgressDisplay()
    {
        if (progressText != null)
            progressText.text = $"EXIT LOCKS  {unlockedCount} / 3";
    }

    private static Bounds GetRenderBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static GameObject CreateColoredCube(string name, Transform parent,
        Vector3 position, Vector3 size, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = size;
        cube.GetComponent<Collider>().enabled = false;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Standard");
        cube.GetComponent<Renderer>().material = new Material(shader) { color = color };
        return cube;
    }

    private static TextMesh CreateText(Transform parent, string value, Vector3 position,
        float scale, Color color)
    {
        GameObject label = new GameObject("Label", typeof(TextMesh));
        label.transform.SetParent(parent, false);
        label.transform.localPosition = position;
        label.transform.localScale = Vector3.one * scale;
        TextMesh text = label.GetComponent<TextMesh>();
        text.text = value;
        text.fontSize = 64;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;
        return text;
    }

    private static void AddDirectInteractorsToHands()
    {
        foreach (NearFarInteractor nearFar in FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
        {
            Transform hand = nearFar.transform.parent;
            if (hand == null || hand.Find("Lab Direct Interactor") != null)
                continue;

            GameObject directObject = new GameObject("Lab Direct Interactor");
            directObject.transform.SetParent(hand, false);
            SphereCollider trigger = directObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.12f;

            XRDirectInteractor direct = directObject.AddComponent<XRDirectInteractor>();
            direct.selectInput = new XRInputButtonReader("Select", "Select Value")
            {
                inputActionReferencePerformed = nearFar.selectInput.inputActionReferencePerformed,
                inputActionReferenceValue = nearFar.selectInput.inputActionReferenceValue,
            };
        }
    }
}
