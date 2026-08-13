using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "PauseManagerSO", menuName = "Scriptable Objects/PauseManagerSO")]
public class PauseManagerSO : ScriptableObject
{
    [SerializeField, ReadOnly] private int _pauseCount = 0;

    public bool isPause
    {
        get
        {
            return _pauseCount > 0;
        }
    }

    public void Pause()
    {
        ++_pauseCount;
    }

    public void Play()
    {
        --_pauseCount;
    }

    public void ResetPauseCount()
    {
        _pauseCount = 0;
    }
}
