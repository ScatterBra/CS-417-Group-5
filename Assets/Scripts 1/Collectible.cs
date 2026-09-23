using System.Collections.Generic;
using UnityEngine;

/// Gems/coins/scrolls. Needs a trigger Collider. Collected when the player's hand or body touches it.
/// Tag your XR Origin (and hands) as "Player".
[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour
{
    public static readonly List<Collectible> All = new List<Collectible>();

    public int value = 1;
    public AudioClip pickupSound;
    public float spinSpeed = 90f;
    public float bobHeight = 0.05f;
    public float bobSpeed = 2f;

    Vector3 startPos;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (!GetComponent<Rigidbody>())
        {
            var rb = gameObject.AddComponent<Rigidbody>();   // needed so trigger events fire
            rb.isKinematic = true;
        }
        startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!(other.CompareTag("Player") || other.transform.root.CompareTag("Player"))) return;
        if (GameManager.Instance) GameManager.Instance.AddCollectible(value);
        if (pickupSound) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        Destroy(gameObject);
    }
}
