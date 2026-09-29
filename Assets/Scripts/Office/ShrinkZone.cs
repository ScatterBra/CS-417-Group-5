using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

/// <summary>
/// A doorway that shrinks the player. Cross it towards the small room and the player
/// shrinks; cross it back and they return to normal size.
///
/// Every change to the player is made at runtime on whatever XR Origin is in the scene,
/// and put back exactly when the player grows again or this object is disabled. Nothing
/// is written to the shared PlayerRig prefab or to its instance in the scene.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class ShrinkZone : MonoBehaviour
{
    [Range(0.02f, 1f)]
    [Tooltip("Player size inside the small room. 0.1 = one tenth of normal height.")]
    public float smallScale = 0.1f;

    [Min(0.05f)]
    public float transitionSeconds = 1.2f;

    [Tooltip("Flat at both ends: the shrink eases in and out.")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Which side of this doorway is the small room, in the collider's local axes.")]
    public Vector3 smallSide = Vector3.left;

    [Min(1f)]
    [Tooltip("XRI already slows movement down with the player's size, so a small player covers the " +
             "same body-lengths per second - and a room that now looks ten times bigger takes ten " +
             "times longer to cross. This speeds a small player back up. 1 = no boost.")]
    public float smallSpeedBoost = 3f;

    [Min(1f)]
    [Tooltip("Jump height shrinks with the player (1.25 m becomes 12.5 cm); this multiplies it back " +
             "up while small. 2 = a 25 cm jump. Keep it well under the lowest platform in the room " +
             "(about 0.9 m) or small players can jump onto furniture.")]
    public float smallJumpBoost = 2f;

    [Header("Keep the furniture out of reach while small")]
    [Min(0.05f)]
    [Tooltip("How far the grab ray reaches while small, in world metres (the stock ray is 10 m). " +
             "0.4 m feels like 4 m to a 1/10 player: long enough to grab things around them on a " +
             "desk, too short to pull anything off a desk top from the floor (about 0.75 m up).")]
    public float smallFarGrabReach = 0.4f;

    [Tooltip("Switch the grab ray off entirely while small, so the hand has to touch what it grabs.")]
    public bool disableFarGrab;

    [Tooltip("Jumping stays on but shrinks with the player (see Small Jump Boost), so it never " +
             "reaches a desk. Tick this to switch jumping off entirely while small.")]
    public bool disableJump;

    [Tooltip("Grab-move lets the player pull themselves through the air.")]
    public bool disableGrabMove = true;

    public UnityEvent onShrunk = new UnityEvent();
    public UnityEvent onRestored = new UnityEvent();

    /// <summary>True from the moment the player starts shrinking until they are back to full size.</summary>
    public bool isSmall { get; private set; }

    /// <summary>Set by <see cref="GrowBackForGood"/>: the doorway no longer shrinks anyone.</summary>
    public bool stayFullSize { get; private set; }

    private BoxCollider doorway;
    private XROrigin origin;
    private CharacterController body;
    private Camera playerCamera;

    // Current size relative to the player's own size (1 = normal).
    private float currentFactor = 1f;
    private Coroutine transition;
    private PlayerSnapshot snapshot;

    private void Awake()
    {
        doorway = GetComponent<BoxCollider>();

        // The player has to be able to walk through the doorway.
        doorway.isTrigger = true;
    }

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Start()
    {
        if (!FindPlayer())
        {
            Debug.LogWarning("ShrinkZone could not find an XR Origin in the scene.", this);
            return;
        }

        // Starting inside the small room (e.g. while testing) means starting small.
        if (SideOf(PlayerFeet()) > 0 && !stayFullSize)
        {
            Shrink(true);
        }
    }

    private void Update()
    {
        if (origin == null && !FindPlayer())
        {
            return;
        }

        int side = SideOf(PlayerFeet());
        if (side > 0 && !isSmall && !stayFullSize)
        {
            Shrink(false);
        }
        else if (side < 0 && isSmall)
        {
            Restore(false);
        }
    }

    private void OnDisable()
    {
        // Never leave the shared rig half-shrunk.
        if (transition != null)
        {
            StopCoroutine(transition);
            transition = null;
        }

        // When the scene is unloading the rig may already be gone; nothing left to restore then.
        if (snapshot != null && origin != null)
        {
            ApplyFactor(1f);
            snapshot.RestoreToggles();
        }

        snapshot = null;
        currentFactor = 1f;

        isSmall = false;
    }

    /// <summary>Shrink the player now. Also callable from a UnityEvent.</summary>
    public void Shrink(bool instant)
    {
        if (origin == null && !FindPlayer())
        {
            return;
        }

        if (snapshot == null)
        {
            snapshot = new PlayerSnapshot(origin, body, playerCamera);
        }

        isSmall = true;
        snapshot.ApplyRestrictions(disableFarGrab, disableJump, disableGrabMove);
        StartTransition(smallScale, instant, onShrunk);
    }

    /// <summary>Return the player to full size. Also callable from a UnityEvent.</summary>
    public void Restore(bool instant)
    {
        if (snapshot == null)
        {
            return;
        }

        isSmall = false;
        StartTransition(1f, instant, onRestored);
    }

    /// <summary>
    /// Grow the player back where they stand, even inside the small room, and never shrink them
    /// again. They end up with exactly their original settings, as with any other restore.
    /// </summary>
    public void GrowBackForGood()
    {
        stayFullSize = true;
        Restore(false);
    }

    private void StartTransition(float target, bool instant, UnityEvent done)
    {
        if (transition != null)
        {
            StopCoroutine(transition);
        }

        if (instant)
        {
            ApplyFactor(target);
            Finish(target, done);
            return;
        }

        transition = StartCoroutine(Transition(target, done));
    }

    private IEnumerator Transition(float target, UnityEvent done)
    {
        float from = currentFactor;
        float elapsed = 0f;
        while (elapsed < transitionSeconds)
        {
            elapsed += Time.deltaTime;
            float eased = ease.Evaluate(Mathf.Clamp01(elapsed / transitionSeconds));

            // Interpolate in log space so each moment of the shrink feels equally big.
            ApplyFactor(Mathf.Exp(Mathf.Lerp(Mathf.Log(from), Mathf.Log(target), eased)));
            yield return null;
        }

        ApplyFactor(target);
        transition = null;
        Finish(target, done);
    }

    private void Finish(float target, UnityEvent done)
    {
        if (Mathf.Approximately(target, 1f) && snapshot != null)
        {
            // Fully back to normal: hand every setting back exactly as it was.
            snapshot.RestoreToggles();
            snapshot = null;
        }

        done.Invoke();
    }

    private void ApplyFactor(float factor)
    {
        if (snapshot == null)
        {
            return;
        }

        // Scale around the player's head, not the rig's pivot, so the player
        // doesn't slide sideways when they're standing away from the pivot.
        Vector3 headBefore = playerCamera != null ? playerCamera.transform.position : origin.transform.position;

        // 0 at full size, 1 at smallScale, even in log space like the shrink itself.
        float smallness = smallScale < 1f ? Mathf.Clamp01(Mathf.Log(factor) / Mathf.Log(smallScale)) : 0f;
        snapshot.ApplyScale(factor,
            Mathf.Lerp(1f, smallSpeedBoost, smallness),
            Mathf.Lerp(1f, smallJumpBoost, smallness),
            smallness, smallFarGrabReach);
        Vector3 headAfter = playerCamera != null ? playerCamera.transform.position : origin.transform.position;
        origin.transform.position += new Vector3(headBefore.x - headAfter.x, 0f, headBefore.z - headAfter.z);

        currentFactor = factor;
    }

    private bool FindPlayer()
    {
        origin = FindFirstObjectByType<XROrigin>();
        if (origin == null)
        {
            return false;
        }

        body = origin.GetComponent<CharacterController>();
        playerCamera = origin.Camera;
        return true;
    }

    private Vector3 PlayerFeet()
    {
        if (body != null && body.enabled)
        {
            Bounds bounds = body.bounds;
            return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        return playerCamera != null ? playerCamera.transform.position : origin.transform.position;
    }

    /// <summary>+1 past the doorway on the small-room side, -1 past it on the other side, 0 inside it.</summary>
    private int SideOf(Vector3 worldPoint)
    {
        Vector3 direction = smallSide.sqrMagnitude > 0f ? smallSide.normalized : Vector3.left;
        Vector3 local = transform.InverseTransformPoint(worldPoint) - doorway.center;
        float distance = Vector3.Dot(local, direction);
        float halfDepth = 0.5f * Mathf.Abs(Vector3.Dot(doorway.size, new Vector3(
            Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z))));

        if (distance > halfDepth)
        {
            return 1;
        }

        return distance < -halfDepth ? -1 : 0;
    }

    /// <summary>
    /// The player's original settings, captured the moment they start shrinking. Settings in
    /// metres that XRI does not already scale with the rig are scaled here; movement speed and
    /// the near-grab radius are left alone because XRI multiplies those by the rig's scale
    /// itself. Restrictions are switched off while small and back on afterwards.
    /// </summary>
    private sealed class PlayerSnapshot
    {
        private readonly Transform originTransform;
        private readonly Vector3 originScale;

        private readonly CharacterController body;
        private readonly float stepOffset;
        private readonly float skinWidth;
        private readonly float minMoveDistance;

        private readonly Camera camera;
        private readonly float nearClip;

        private readonly List<(ContinuousMoveProvider provider, float speed)> movers = new List<(ContinuousMoveProvider, float)>();
        private readonly List<(JumpProvider provider, float height)> jumps = new List<(JumpProvider, float)>();
        private readonly List<(GravityProvider provider, float buffer)> gravities = new List<(GravityProvider, float)>();
        private readonly List<(XRPokeInteractor poke, float depth, float width, float selectWidth, float hoverRadius)> pokes =
            new List<(XRPokeInteractor, float, float, float, float)>();
        private readonly List<(Canvas canvas, float planeDistance)> headCanvases = new List<(Canvas, float)>();

        private readonly List<(CurveInteractionCaster caster, float distance)> farRays = new List<(CurveInteractionCaster, float)>();
        private readonly List<(CurveVisualController visual, float maxDistance, float restingLength)> rayVisuals =
            new List<(CurveVisualController, float, float)>();
        private readonly List<(LineRenderer line, float width)> rayLines = new List<(LineRenderer, float)>();

        private readonly List<(NearFarInteractor interactor, bool farCasting)> farGrabbers = new List<(NearFarInteractor, bool)>();
        private readonly List<(Behaviour behaviour, bool enabled)> toggled = new List<(Behaviour, bool)>();

        public PlayerSnapshot(XROrigin origin, CharacterController body, Camera camera)
        {
            originTransform = origin.transform;
            originScale = originTransform.localScale;

            this.body = body;
            if (body != null)
            {
                stepOffset = body.stepOffset;
                skinWidth = body.skinWidth;
                minMoveDistance = body.minMoveDistance;
            }

            this.camera = camera;
            if (camera != null)
            {
                nearClip = camera.nearClipPlane;
            }

            foreach (ContinuousMoveProvider mover in origin.GetComponentsInChildren<ContinuousMoveProvider>(true))
            {
                movers.Add((mover, mover.moveSpeed));
            }

            foreach (JumpProvider jump in origin.GetComponentsInChildren<JumpProvider>(true))
            {
                jumps.Add((jump, jump.jumpHeight));
            }

            foreach (GravityProvider gravity in origin.GetComponentsInChildren<GravityProvider>(true))
            {
                gravities.Add((gravity, gravity.sphereCastDistanceBuffer));
            }

            foreach (XRPokeInteractor poke in origin.GetComponentsInChildren<XRPokeInteractor>(true))
            {
                pokes.Add((poke, poke.pokeDepth, poke.pokeWidth, poke.pokeSelectWidth, poke.pokeHoverRadius));
            }

            foreach (Canvas canvas in origin.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    headCanvases.Add((canvas, canvas.planeDistance));
                }
            }

            foreach (NearFarInteractor interactor in origin.GetComponentsInChildren<NearFarInteractor>(true))
            {
                farGrabbers.Add((interactor, interactor.enableFarCasting));
                if (interactor.farInteractionCaster is CurveInteractionCaster caster)
                {
                    farRays.Add((caster, caster.castDistance));
                }
            }

            foreach (CurveVisualController visual in origin.GetComponentsInChildren<CurveVisualController>(true))
            {
                rayVisuals.Add((visual, visual.maxVisualCurveDistance, visual.restingVisualLineLength));
                LineRenderer line = visual.GetComponent<LineRenderer>();
                if (line != null)
                {
                    rayLines.Add((line, line.widthMultiplier));
                }
            }
        }

        /// <param name="smallness">0 at full size, 1 fully small.</param>
        /// <param name="farReach">Grab ray length in world metres when fully small.</param>
        public void ApplyScale(float factor, float speedMultiplier, float jumpMultiplier, float smallness, float farReach)
        {
            originTransform.localScale = originScale * factor;

            if (body != null)
            {
                body.stepOffset = stepOffset * factor;
                body.minMoveDistance = minMoveDistance * factor;

                // XRI keeps the capsule centre at height / 2 + skin width, but only recomputes it
                // when the body moves. Keep that true now, so a player who stands still while
                // growing back ends up with exactly the centre XRI would give them.
                float previousSkin = body.skinWidth;
                body.skinWidth = skinWidth * factor;
                Vector3 centre = body.center;
                centre.y += body.skinWidth - previousSkin;
                body.center = centre;
            }

            if (camera != null)
            {
                camera.nearClipPlane = nearClip * factor;
            }

            // XRI already multiplies moveSpeed by the rig's scale; only the boost goes on top.
            foreach (var (provider, speed) in movers)
            {
                provider.moveSpeed = speed * speedMultiplier;
            }

            // Jump height is not scaled by XRI, so it would stay 1.25 m - taller than a desk.
            foreach (var (provider, height) in jumps)
            {
                provider.jumpHeight = height * factor * jumpMultiplier;
            }

            // The controller rests a skin width (world units) above the floor, but XRI offsets the
            // capsule by that same number in the rig's local units. Scaled down, the difference
            // lifts the rig by skin * (1 - factor) in local units - enough for the ground check,
            // which is tuned for full size, to miss the floor. Without it the player never counts
            // as grounded and cannot jump. Reach that much further.
            foreach (var (provider, buffer) in gravities)
            {
                provider.sphereCastDistanceBuffer = buffer + skinWidth * (1f - factor);
            }

            foreach (var (poke, depth, width, selectWidth, hoverRadius) in pokes)
            {
                poke.pokeDepth = depth * factor;
                poke.pokeWidth = width * factor;
                poke.pokeSelectWidth = selectWidth * factor;
                poke.pokeHoverRadius = hoverRadius * factor;
            }

            // XRI does not scale the grab ray. Left at 10 m it would reach every desk top from
            // the floor, so shorten it (in log space, like the shrink) to a room-sized reach.
            foreach (var (caster, distance) in farRays)
            {
                caster.castDistance = Mathf.Exp(Mathf.Lerp(Mathf.Log(distance), Mathf.Log(Mathf.Min(farReach, distance)), smallness));
            }

            // The ray's line is drawn in world units too: keep it the same thickness to the player.
            foreach (var (visual, maxDistance, restingLength) in rayVisuals)
            {
                visual.maxVisualCurveDistance = Mathf.Exp(Mathf.Lerp(Mathf.Log(maxDistance), Mathf.Log(Mathf.Min(farReach, maxDistance)), smallness));
                visual.restingVisualLineLength = restingLength * factor;
            }

            foreach (var (line, width) in rayLines)
            {
                line.widthMultiplier = width * factor;
            }

            // The score HUD floats a fixed distance in front of the eyes; bring it in
            // so it isn't hidden behind furniture that is now right in the player's face.
            foreach (var (canvas, planeDistance) in headCanvases)
            {
                canvas.planeDistance = planeDistance * factor;
            }
        }

        public void ApplyRestrictions(bool disableFarGrab, bool disableJump, bool disableGrabMove)
        {
            if (toggled.Count > 0)
            {
                return;
            }

            if (disableFarGrab)
            {
                foreach (var (interactor, _) in farGrabbers)
                {
                    interactor.enableFarCasting = false;
                }
            }

            Transform root = originTransform;
            if (disableJump)
            {
                foreach (JumpProvider jump in root.GetComponentsInChildren<JumpProvider>(true))
                {
                    Toggle(jump);
                }
            }

            if (disableGrabMove)
            {
                foreach (GrabMoveProvider grabMove in root.GetComponentsInChildren<GrabMoveProvider>(true))
                {
                    Toggle(grabMove);
                }

                foreach (TwoHandedGrabMoveProvider twoHanded in root.GetComponentsInChildren<TwoHandedGrabMoveProvider>(true))
                {
                    Toggle(twoHanded);
                }
            }

            // Marks the restrictions as applied even when nothing was toggled.
            toggled.Add((null, false));
        }

        public void RestoreToggles()
        {
            foreach (var (interactor, farCasting) in farGrabbers)
            {
                if (interactor != null)
                {
                    interactor.enableFarCasting = farCasting;
                }
            }

            foreach (var (behaviour, enabled) in toggled)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = enabled;
                }
            }

            toggled.Clear();
        }

        private void Toggle(Behaviour behaviour)
        {
            toggled.Add((behaviour, behaviour.enabled));
            behaviour.enabled = false;
        }
    }
}
