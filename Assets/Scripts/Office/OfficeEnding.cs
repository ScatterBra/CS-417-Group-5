using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// The end of the office run. Once every disk is in the Look_Box the guide arrow fades in and
/// stays. The player jumps off the desk into the grow-back volume, grows back to full size where
/// they land, and walks back to the first room; stepping into it opens the sliding door and
/// unlocks scene travel.
///
/// The player is returned to exactly their original settings by <see cref="ShrinkZone"/>, which
/// then never shrinks them again, so they leave for the next scene as the shared rig expects.
/// </summary>
[DisallowMultipleComponent]
public sealed class OfficeEnding : MonoBehaviour
{
    [Header("Room 2")]
    [Tooltip("The Look_Box counter. The ending starts when it unlocks.")]
    public EscapeController diskLocks;

    [Tooltip("Hidden until the disks are in, then fades in and keeps gently pulsing.")]
    public GameObject arrow;

    [Min(0.05f)]
    public float arrowFadeSeconds = 0.8f;

    [Range(0f, 1f)]
    [Tooltip("Lowest opacity of the pulse after the arrow has faded in.")]
    public float arrowPulseMinAlpha = 0.55f;

    [Min(0.1f)]
    public float arrowPulseSeconds = 1.4f;

    [Tooltip("Touching this volume grows the player back. Ignored until the arrow shows.")]
    public BoxCollider growBackVolume;

    public ShrinkZone shrinkZone;

    [Tooltip("Switched off once the player is full size again, e.g. the sticky-note gates, " +
             "which are built for a small player.")]
    public Behaviour[] disableWhenGrown = new Behaviour[0];

    [Header("Room 1")]
    [Tooltip("Walking into this volume (full size) opens the door.")]
    public BoxCollider roomOneArea;

    [Tooltip("Room 1's door controller.")]
    public EscapeController exitDoor;

    [Tooltip("The team's scene travel at the exit. Locked until the door opens.")]
    public SceneLoader exitTravel;

    [Tooltip("Marked complete in GameProgress when the door opens. Blank = this scene's path, " +
             "the same default SceneLoader uses.")]
    public string roomId;

    public UnityEvent onArrowShown = new UnityEvent();
    public UnityEvent onGrownBack = new UnityEvent();
    public UnityEvent onDoorOpened = new UnityEvent();

    private enum Stage { WaitingForDisks, ArrowShown, GrowingBack, WalkingBack, Done }

    private Stage stage;
    private XROrigin origin;
    private CharacterController body;
    private readonly List<(SpriteRenderer renderer, Color color)> arrowSprites = new List<(SpriteRenderer, Color)>();
    private float arrowShownTime;

    private string RoomId => string.IsNullOrWhiteSpace(roomId) ? gameObject.scene.path : roomId.Trim();

    private void Start()
    {
        if (arrow != null)
        {
            foreach (SpriteRenderer sprite in arrow.GetComponentsInChildren<SpriteRenderer>(true))
            {
                arrowSprites.Add((sprite, sprite.color));
            }

            arrow.SetActive(false);
        }

        if (exitTravel != null)
        {
            exitTravel.SetUnlocked(false);
        }
    }

    private void Update()
    {
        if (stage != Stage.WaitingForDisks)
        {
            AnimateArrow();
        }

        switch (stage)
        {
            case Stage.WaitingForDisks:
                if (diskLocks != null && diskLocks.isUnlocked)
                {
                    ShowArrow();
                }

                break;

            case Stage.ArrowShown:
                if (PlayerTouches(growBackVolume))
                {
                    GrowBack();
                }

                break;

            case Stage.WalkingBack:
                if (PlayerTouches(roomOneArea))
                {
                    OpenDoor();
                }

                break;
        }
    }

    /// <summary>Show the arrow now. Also callable from a UnityEvent while testing.</summary>
    public void ShowArrow()
    {
        if (stage != Stage.WaitingForDisks)
        {
            return;
        }

        stage = Stage.ArrowShown;
        arrowShownTime = Time.time;
        if (arrow != null)
        {
            arrow.SetActive(true);
            AnimateArrow();
        }

        onArrowShown.Invoke();
    }

    /// <summary>Grow the player back where they are. Also callable from a UnityEvent while testing.</summary>
    public void GrowBack()
    {
        if (stage != Stage.ArrowShown && stage != Stage.WaitingForDisks)
        {
            return;
        }

        stage = Stage.GrowingBack;
        if (shrinkZone != null && shrinkZone.isActiveAndEnabled && shrinkZone.isSmall)
        {
            shrinkZone.onRestored.AddListener(OnGrownBack);
            shrinkZone.GrowBackForGood();
        }
        else
        {
            if (shrinkZone != null)
            {
                shrinkZone.GrowBackForGood();
            }

            OnGrownBack();
        }
    }

    private void OnGrownBack()
    {
        if (stage != Stage.GrowingBack)
        {
            return;
        }

        stage = Stage.WalkingBack;
        if (shrinkZone != null)
        {
            shrinkZone.onRestored.RemoveListener(OnGrownBack);
        }

        foreach (Behaviour behaviour in disableWhenGrown)
        {
            if (behaviour != null)
            {
                behaviour.enabled = false;
            }
        }

        onGrownBack.Invoke();
    }

    /// <summary>Open the sliding door and unlock travel. Also callable from a UnityEvent while testing.</summary>
    public void OpenDoor()
    {
        if (stage == Stage.Done)
        {
            return;
        }

        stage = Stage.Done;
        GameProgress.CompleteRoom(RoomId);
        if (exitDoor != null)
        {
            exitDoor.UnlockNow();
        }

        if (exitTravel != null)
        {
            exitTravel.SetUnlocked(true);
        }

        onDoorOpened.Invoke();
    }

    private void AnimateArrow()
    {
        if (arrowSprites.Count == 0)
        {
            return;
        }

        float age = Time.time - arrowShownTime;
        float alpha;
        if (age < arrowFadeSeconds)
        {
            // Ease in from nothing.
            float t = age / arrowFadeSeconds;
            alpha = t * t * (3f - 2f * t);
        }
        else
        {
            // Then breathe between full and the pulse minimum so it keeps catching the eye.
            float wave = 0.5f + 0.5f * Mathf.Cos((age - arrowFadeSeconds) * 2f * Mathf.PI / arrowPulseSeconds);
            alpha = Mathf.Lerp(arrowPulseMinAlpha, 1f, wave);
        }

        foreach (var (sprite, color) in arrowSprites)
        {
            if (sprite != null)
            {
                sprite.color = new Color(color.r, color.g, color.b, color.a * alpha);
            }
        }
    }

    private bool PlayerTouches(BoxCollider volume)
    {
        if (volume == null || !FindPlayer() || !body.enabled)
        {
            return false;
        }

        // Test the body's capsule against the volume: bottom, middle and top of its spine.
        float scale = body.transform.lossyScale.y;
        float radius = body.radius * scale;
        Vector3 centre = body.transform.TransformPoint(body.center);
        float spine = Mathf.Max(0f, body.height * scale * 0.5f - radius);
        Vector3 up = body.transform.up;
        foreach (Vector3 point in new[] { centre - up * spine, centre, centre + up * spine })
        {
            if ((volume.ClosestPoint(point) - point).sqrMagnitude <= radius * radius)
            {
                return true;
            }
        }

        return false;
    }

    private bool FindPlayer()
    {
        if (origin == null)
        {
            origin = FindFirstObjectByType<XROrigin>();
            if (origin == null) return false;
            body = origin.GetComponent<CharacterController>();
        }

        return body != null;
    }
}
