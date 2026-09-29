using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "PauseManagerSO", menuName = "Scriptable Objects/PauseManagerSO")]
public class PauseManagerSO : ScriptableObject
{
    [SerializeField] private TimeScaleManagerSO timeScaleManager;
    [SerializeField, ReadOnly] private int _pauseCount = 0;

    public bool isPause
    {
        get
        {
            return _pauseCount > 0;
        }
    }

    public UnityEvent<bool> pauseChangedEvent = new();

    /// <summary>일시정지 요청을 추가. 첫 요청일 때 게임을 정지</summary>
    public void Pause()
    {
        ++_pauseCount;
        if (_pauseCount == 1)
            OnPauseChanged();
    }

    /// <summary>일시정지 요청을 해제. 남은 요청이 없으면 게임을 재개</summary>
    public void Play()
    {
        if (_pauseCount <= 0)
        {
            Debug.LogWarning("PauseManagerSO: Pause 요청 없이 Play가 호출되었습니다.");
            return;
        }

        --_pauseCount;
        if (_pauseCount == 0)
            OnPauseChanged();
    }

    /// <summary>모든 일시정지 요청을 초기화하고 게임을 재개</summary>
    public void ResetPauseCount()
    {
        _pauseCount = 0;
        OnPauseChanged();
    }

    /// <summary>일시정지 상태를 TimeScaleManagerSO에 전달하고 변경 이벤트를 호출</summary>
    private void OnPauseChanged()
    {
        timeScaleManager.SetPause(isPause);
        pauseChangedEvent?.Invoke(isPause);
    }
}
