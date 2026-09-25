using UnityEngine;
using TMPro;

public class DoorConsole : MonoBehaviour
{
    public TMP_Text consoleText;
    public void SetKeyCount(int count)
    {
        consoleText.text = "Door: LOCKED\nKeys: " + count + "/3";
    }
    public void SetUnlocked()
    {
        consoleText.text = "Door: UNLOCKED\nKeys: 3/3";
    }
    
}
