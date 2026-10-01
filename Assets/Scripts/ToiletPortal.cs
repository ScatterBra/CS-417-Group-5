using UnityEngine;

public class ToiletPortal : MonoBehaviour
{
    public GameObject portal;

    public void Reveal()
    {
        portal.SetActive(true);
    }
}