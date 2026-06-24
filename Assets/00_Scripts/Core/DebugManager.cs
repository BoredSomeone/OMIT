using UnityEngine;

public class DebugManager : MonoBehaviour
{
    public bool backgroundPlay;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Application.runInBackground = backgroundPlay;
    }
}
