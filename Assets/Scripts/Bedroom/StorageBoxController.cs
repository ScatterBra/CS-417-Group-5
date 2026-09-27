using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StorageBoxController : MonoBehaviour
{
    [Header("Physics Setup")]
    [Tooltip("The Rigidbody attached to the physical lid.")]
    public Rigidbody lidRigidbody;
    
    [Tooltip("The Grab Interactable on the lid, left disabled in the Inspector.")]
    public XRGrabInteractable lidGrabInteractable; 
    
    [Tooltip("The Fixed Joint holding the lid to the base.")]
    public FixedJoint lidJoint;

    [Header("Audio")]
    [Tooltip("The sound to play when the lid pops off.")]
    public AudioSource popAudioSource;

    [Header("Forces")]
    public float popForce = 5f;
    public Vector3 popTorque = new Vector3(2f, 0f, 0f);

    private bool isOpened = false;

    public void OpenBox()
    {
        if (!isOpened && lidRigidbody != null)
        {
            isOpened = true;

            // Play the mechanical pop/hiss sound
            if (popAudioSource != null)
            {
                popAudioSource.Play();
            }

            // Destroy the joint to break the physical lock between lid and base
            if (lidJoint != null)
            {
                Destroy(lidJoint);
            }

            // Unparent the lid so it flies independently of the base
            lidRigidbody.transform.SetParent(null); 

            // Enable grabbing so the player can catch or pick it up
            if (lidGrabInteractable != null)
            {
                lidGrabInteractable.interactionLayers = InteractionLayerMask.GetMask("Default");
            }

            // Blast lid upward
            lidRigidbody.AddForce(Vector3.up * popForce, ForceMode.Impulse);
            lidRigidbody.AddTorque(popTorque, ForceMode.Impulse);
        }
    }
}