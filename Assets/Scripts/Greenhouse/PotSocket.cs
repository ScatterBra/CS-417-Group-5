using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PotSocket : MonoBehaviour
{
    public PotShape acceptedShape;
    public PotPuzzleManager puzzleManager;
    private XRSocketInteractor socket;

    private void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
    }

    private void OnEnable()
    {
        socket.selectEntered.AddListener(OnPotInsert);
        socket.selectExited.AddListener(OnPotRemove);
    }

    private void OnDisable()
    {
        socket.selectEntered.RemoveListener(OnPotInsert);
        socket.selectExited.RemoveListener(OnPotRemove);
    }

    private void OnPotInsert(SelectEnterEventArgs args)
    {
        Pot pot = args.interactableObject.transform.GetComponent<Pot>();

        if (pot != null)
        {
            puzzleManager.CheckPuzzle();
        }
    }

    private void OnPotRemove(SelectExitEventArgs args)
    {
        puzzleManager.CheckPuzzle();
    }
}