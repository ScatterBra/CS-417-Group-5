using UnityEngine;

public class DoorManager : MonoBehaviour
{
    public GameObject door;
    public DoorConsole console;

    private bool blueKey;
    private bool greenKey;
    private bool yellowKey;

    public void KeycardInsert(KeycardColor color)
    {
        switch (color)
        {
            case KeycardColor.Blue:
                blueKey = true;
                break;
            case KeycardColor.Green:
                greenKey = true;
                break;
            case KeycardColor.Yellow:
                yellowKey = true;
                break;
        }
        UpdateConsole();
        if (AllKeysInserted())
        {
            UnlockDoor();
        }
    }

    private bool AllKeysInserted()
    {
        return blueKey && greenKey && yellowKey;
    }

    private void UnlockDoor()
    {
        Debug.Log("UNLOCKED UNLOCKED UNLOCKED");
        if (door != null)
        {
            door.SetActive(false);
        }

        if (console != null)
        {
            console.SetUnlocked();
        }
    }

    private void UpdateConsole()
    {
        int count = 0;
        if (blueKey) count++;
        if (greenKey) count++;
        if (yellowKey) count++;

        if(console!= null)
        {
            console.SetKeyCount(count);
        }

    }
}
