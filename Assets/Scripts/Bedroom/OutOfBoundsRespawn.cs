using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class OutOfBoundsRespawn : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("The world Y-coordinate floor. If the object falls below this, it respawns.")]
    [SerializeField] private float fallThresholdY = -2.5f;
    
    [Tooltip("The world Y-coordinate ceiling. If the object flies above this, it respawns.")]
    [SerializeField] private float ceilingThresholdY = 2.5f;
    
    [Tooltip("Optional: The moving parent container (like a drawer). The item will respawn relative to this object's current position.")]
    [SerializeField] private Transform referenceContainer;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (referenceContainer != null)
        {
            initialPosition = referenceContainer.InverseTransformPoint(transform.position);
            initialRotation = Quaternion.Inverse(referenceContainer.rotation) * transform.rotation;
        }
        else
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }
    }

    void Update()
    {
        // Check if the object is outside either the upper or lower bounds
        if (transform.position.y < fallThresholdY || transform.position.y > ceilingThresholdY)
        {
            Respawn();
        }
    }

    public void Respawn()
    {
        if (referenceContainer != null)
        {
            transform.position = referenceContainer.TransformPoint(initialPosition);
            transform.rotation = referenceContainer.rotation * initialRotation;
        }
        else
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; 
            rb.angularVelocity = Vector3.zero;
        }
    }
}