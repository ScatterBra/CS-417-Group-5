using UnityEngine;

public class ToiletPortal : MonoBehaviour
{
    public GameObject portal;

    void Start()
    {
        portal.SetActive(false);
    }

    public void Reveal()
    {
        portal.SetActive(true);
    }
}