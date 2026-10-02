using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ResetBox : MonoBehaviour
{
    private bool isHovered = false;
    private XRSimpleInteractable interactable;

    [Header("Input")]
    [SerializeField] private InputActionReference resetButtonAction;

    void Start()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        
        // Listen to the XR toolkit's hover events
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
    }

    void Update()
    {
        if (isHovered && resetButtonAction != null && resetButtonAction.action.WasPressedThisFrame())
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
        }
    }
    
    private void OnHoverEnter(HoverEnterEventArgs args) => isHovered = true;
    private void OnHoverExit(HoverExitEventArgs args) => isHovered = false;


    void OnEnable()
    {
        if (resetButtonAction != null && resetButtonAction.action != null) 
            resetButtonAction.action.Enable();
    }

    void OnDisable()
    {
        if (resetButtonAction != null && resetButtonAction.action != null) 
            resetButtonAction.action.Disable();
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEnter);
            interactable.hoverExited.RemoveListener(OnHoverExit);
        }
    }
}
