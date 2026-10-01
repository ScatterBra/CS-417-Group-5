using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class BathroomDoor : MonoBehaviour
{
    public SceneLoader loader;

    public GameObject door;

    
    public string[] requiredRoomIds = { "greenhouse", "CaptainsQuarters", "lab", "controlroom", "office" };

    public bool IsUnlocked { get; private set; }

    void Awake()
    {
        if (!loader) loader = GetComponent<SceneLoader>();
    }

    void Start()
    {
        Refresh();
    }

    void Update()
    {
        if (!IsUnlocked) Refresh();
    }

    void Refresh()
    {
        bool allDone = AllRoomsCompleted();
        if (allDone == IsUnlocked) return;

        IsUnlocked = allDone;
        if (loader) loader.SetUnlocked(allDone);

        if (allDone)
        {
            door.GetComponent<Light>().color = Color.green; 
        }
    }

    bool AllRoomsCompleted()
    {
        foreach (string id in requiredRoomIds)
        {
            if (!GameProgress.IsRoomCompleted(id))
                return false;
        }
        return requiredRoomIds.Length > 0;
    }
}