using UnityEngine;


/// Put on the flashlight. While held (or always, if not grabbed), it checks every InvisibleWriting
/// object in the scene and reveals the ones it's currently pointed at, within range and cone angle.
/// This is what makes writing depend on the BLACKLIGHT'S direction, not the player's view direction.
public class BlacklightProp : MonoBehaviour
{
    public Transform beamOrigin;         // defaults to this transform; put an empty at the lamp tip if you have one
    public float maxDistance = 5f;
    [Range(1f, 90f)] public float coneAngle = 20f;
    public Light spotLight;              // optional: an actual Spot Light for the visual beam
    public Color onColor = new Color(0.6f, 0.2f, 1f);

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    void Awake()
    {
        if (!beamOrigin) beamOrigin = transform;
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (spotLight)
        {
            spotLight.color = onColor;
            spotLight.spotAngle = coneAngle * 2f;
            spotLight.range = maxDistance;
        }
    }

    void Update()
    {
        foreach (var w in InvisibleWriting.All)
        {
            Vector3 toTarget = w.transform.position - beamOrigin.position;
            float dist = toTarget.magnitude;
            if (dist > maxDistance) continue;

            float angle = Vector3.Angle(beamOrigin.forward, toTarget);
            if (angle > coneAngle) continue;

            w.Illuminate();
        }
    }
}
