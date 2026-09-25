using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Keylock : MonoBehaviour
{
    public KeycardColor acceptedColor;
    public DoorManager doorManager;
    public Renderer lockRenderer;
    private XRSocketInteractor socket;

    private void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
    }

    private void OnEnable()
    {
        socket.selectEntered.AddListener(OnInsert);
    }
    
    private void OnDisable()
    {
        socket.selectEntered.RemoveListener(OnInsert);
    }

    private void OnInsert(SelectEnterEventArgs args)
    {
        Keycard keycard = args.interactableObject.transform.GetComponent<Keycard>();
        //checks if keycard inserted is correct
        if (keycard == null)
        {
            return;
        }
        if (keycard.color == acceptedColor)
        {
            doorManager.KeycardInsert(acceptedColor);
            lockRenderer.material.color = Color.red;

        }

    }

}