using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PotPuzzleManager : MonoBehaviour
{
    public XRSocketInteractor squareSocket;
    public XRSocketInteractor roundSocket;

    public GameObject hologram;
    private bool solved = false;

    public void CheckPuzzle()
    {
        //make sure socket isnt empty
        if (solved) return;
        if (!squareSocket.hasSelection || !roundSocket.hasSelection) return;

        Pot squarePot = squareSocket.firstInteractableSelected.transform.GetComponent<Pot>();
        Pot roundPot = roundSocket.firstInteractableSelected.transform.GetComponent<Pot>();

        if (squarePot == null || roundPot == null) return;

        //check pot shape
        if (squarePot.potShape == PotShape.Square && roundPot.potShape == PotShape.Round)
        {
            SolvePuzzle();
        }
    }

    private void SolvePuzzle()
    {
        solved = true;
        Debug.Log("POT PUZZLE SOLVED! POT PUZZLE SOLVED! POT PUZZLE SOLVED!");

        if (hologram != null)
        {
            hologram.SetActive(false);
        }
    }
}