using UnityEngine;

public class PortalTrigger : MonoBehaviour
{
    public SceneLoader loader;

    bool triggered;

    void Awake()
    {
        if (!loader) loader = GetComponent<SceneLoader>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;
        loader.LoadTargetScene();
    }
}