using UnityEngine;
using UnityEngine.InputSystem;

public class Drawer : MonoBehaviour
{
    [SerializeField] GameObject drawer;
    [SerializeField] private Transform drawerTransform;
    [SerializeField] private bool isOpen = false;

    [Header("Drawer LockingSettings")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private GameObject key;    // If the drawer is locked, this will be the key object that can unlock it
    [SerializeField] private GameObject lockForKey;   // If the drawer is locked, this will be the lock object that is displayed on the drawer
    [SerializeField] private InputActionReference openDrawerAction; // The input action that will be used to open the drawer

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        drawerTransform = GetComponent<Transform>();
        
    }

    
}
