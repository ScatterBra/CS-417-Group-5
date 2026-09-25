using System.Collections;
using UnityEngine;

/// <summary>
/// Plays this object's clip again and again with a gap between plays, like a phone ringing
/// for the player's attention. Waits for <see cref="Begin"/> (e.g. from a UnityEvent) unless
/// Play On Start is ticked, then keeps going until <see cref="Stop"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class RepeatingSound : MonoBehaviour
{
    [Min(0.05f)]
    [Tooltip("Seconds from the start of one play to the start of the next.")]
    public float interval = 1f;

    [Tooltip("Start with the scene. Off = stay silent until Begin() is called.")]
    public bool playOnStart;

    private AudioSource source;
    private Coroutine ringing;

    public bool isRinging => ringing != null;

    private void Awake()
    {
        source = GetComponent<AudioSource>();

        // The gap comes from here, not from the source looping.
        source.loop = false;
    }

    private void Start()
    {
        if (playOnStart)
        {
            Begin();
        }
    }

    private void OnDisable()
    {
        // Disabling stops the coroutine; forget it so Begin() works again later.
        ringing = null;
    }

    /// <summary>Start ringing. Does nothing if already ringing. Safe to call from a UnityEvent.</summary>
    public void Begin()
    {
        if (ringing != null || !isActiveAndEnabled)
        {
            return;
        }

        ringing = StartCoroutine(Ring());
    }

    /// <summary>Stop ringing. Safe to call from a UnityEvent.</summary>
    public void Stop()
    {
        if (ringing != null)
        {
            StopCoroutine(ringing);
            ringing = null;
        }
    }

    private IEnumerator Ring()
    {
        var wait = new WaitForSeconds(interval);
        while (true)
        {
            source.Play();
            yield return wait;
        }
    }
}
