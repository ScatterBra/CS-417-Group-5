using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class GrabbableProp : MonoBehaviour
{
    [Header("Identity")]
    public bool isKey = true;               // false = red herring
    public string keyId = "";               // must match Lock.requiredKeyId (e.g. "RedGem")

    [Header("Grab signifier (hover glow)")]
    public bool glowOnHover = true;
    public Color glowColor = new Color(1f, 0.85f, 0.2f);
    public float glowIntensity = 1.5f;

    [Header("Safety")]
    public float respawnBelowY = -5f;       // props that fall below this go back to their start

    XRGrabInteractable grab;
    Rigidbody rb;
    Material[] mats;
    Color[] originalEmission;
    Vector3 startPos;
    Quaternion startRot;
    bool discovered;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        grab.throwOnDetach = true;
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        if (GetComponentInChildren<Collider>() == null)
            Debug.LogWarning($"{name}: no Collider found, so it can't be grabbed or collide.", this);

        var list = new List<Material>();
        foreach (var r in GetComponentsInChildren<Renderer>())
            foreach (var m in r.materials) list.Add(m);
        mats = list.ToArray();
        originalEmission = new Color[mats.Length];
        for (int i = 0; i < mats.Length; i++)
            originalEmission[i] = mats[i].HasProperty("_EmissionColor") ? mats[i].GetColor("_EmissionColor") : Color.black;

        startPos = transform.position;
        startRot = transform.rotation;

        grab.hoverEntered.AddListener(_ => SetGlow(true));
        grab.hoverExited.AddListener(_ => SetGlow(false));
        grab.selectEntered.AddListener(_ => OnGrabbed());
    }

    void OnGrabbed()
    {
        SetGlow(false);
        if (discovered || !isKey) return;
        discovered = true;
        if (GameManager.Instance)
            GameManager.Instance.KeyDiscovered(string.IsNullOrEmpty(keyId) ? name : keyId);
    }

    void SetGlow(bool on)
    {
        if (!glowOnHover) return;
        for (int i = 0; i < mats.Length; i++)
        {
            if (!mats[i].HasProperty("_EmissionColor")) continue;
            if (on)
            {
                mats[i].EnableKeyword("_EMISSION");
                mats[i].SetColor("_EmissionColor", glowColor * glowIntensity);
            }
            else mats[i].SetColor("_EmissionColor", originalEmission[i]);
        }
    }

    void Update()
    {
        if (transform.position.y < respawnBelowY && !grab.isSelected) Respawn();
    }

    void Respawn()
    {
        if (!rb.isKinematic)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(startPos, startRot);
    }
}
