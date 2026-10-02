using UnityEngine;

public class SyringeSpawner : MonoBehaviour
{
    public GameObject syringe;

    public string requiredRoomId = "greenhouse";


    void Start()
    {
        bool solved = GameProgress.IsRoomCompleted(requiredRoomId);
        if (syringe) syringe.SetActive(solved);
    }
}