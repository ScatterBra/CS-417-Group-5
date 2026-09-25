using System.Collections;
using UnityEngine;

public class CardScanner : MonoBehaviour
{
    [SerializeField] private Drawer connectedDrawer;
    [SerializeField] private GameObject correctKeyCard;
    
    [Header("Scanner Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip processingBeep;
    [SerializeField] private AudioClip successBeep;
    [SerializeField] private AudioClip failBeep;

    [Header("Scanner Visuals")]
    [SerializeField] private MeshRenderer scannerMeshRenderer;
    [SerializeField] private Material idleMaterial;       
    [SerializeField] private Material processingMaterial; 
    [SerializeField] private Material successMaterial;    

    private Coroutine scanCoroutine;
    private bool isUnlocked = false; // Tracks if we are in the 5-second window

    void Start()
    {
        if (scannerMeshRenderer != null && idleMaterial != null) 
            scannerMeshRenderer.material = idleMaterial;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don't scan again if it's already in the 5-second unlocked window
        if (isUnlocked) return;

        GameObject hitObject = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;

        if (hitObject == correctKeyCard)
        {
            scanCoroutine = StartCoroutine(ScanRoutine());
        }
        else if (other.attachedRigidbody != null) 
        {
            if (audioSource && failBeep) audioSource.PlayOneShot(failBeep);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // If it's already unlocked, pulling the card away shouldn't cancel the 5-second timer
        if (isUnlocked) return;

        GameObject hitObject = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;

        if (hitObject == correctKeyCard && scanCoroutine != null)
        {
            StopCoroutine(scanCoroutine);
            scanCoroutine = null;
            
            if (scannerMeshRenderer != null) scannerMeshRenderer.material = idleMaterial;
        }
    }

    private IEnumerator ScanRoutine()
    {
        if (scannerMeshRenderer != null) scannerMeshRenderer.material = processingMaterial;
        if (audioSource && processingBeep) audioSource.PlayOneShot(processingBeep);

        // Wait for exactly 1 continuous second of contact
        yield return new WaitForSeconds(1f);

        // Lock in the success state
        isUnlocked = true;
        if (scannerMeshRenderer != null) scannerMeshRenderer.material = successMaterial;
        if (audioSource && successBeep) audioSource.PlayOneShot(successBeep);
        if (connectedDrawer != null) connectedDrawer.Unlock();

        // Wait 5 seconds for the player to open the drawer
        yield return new WaitForSeconds(5f);

        // Auto-lock and reset visuals
        if (scannerMeshRenderer != null) scannerMeshRenderer.material = idleMaterial;
        if (connectedDrawer != null) connectedDrawer.Lock();
        
        isUnlocked = false;
        scanCoroutine = null;
    }
}