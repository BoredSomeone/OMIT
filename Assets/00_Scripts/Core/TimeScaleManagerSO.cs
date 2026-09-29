using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Time.timeScale을 변경하는 유일한 관리자.
/// 요청 주체(source)별 배율을 곱해 적용하고, 일시정지 중에는 배율과 무관하게 0을 적용한다.
/// </summary>
[CreateAssetMenu(fileName = "TimeScaleManagerSO", menuName = "Scriptable Objects/TimeScaleManagerSO")]
public class TimeScaleManagerSO : ScriptableObject
{
    [SerializeField, ReadOnly] private bool _isPause = false;
    [SerializeField, ReadOnly] private float _scale = 1f;
    [SerializeField, ReadOnly] private int _modifierCount = 0;

    private readonly Dictionary<object, float> modifiers = new();

    /// <summary>일시정지를 제외한, 배율만 반영된 현재 시간 배율</summary>
    public float scale { get { return _scale; } }

    /// <summary>일시정지 여부를 설정하고 timeScale을 재적용 (PauseManagerSO에서 호출)</summary>
    public void SetPause(bool isPause)
    {
        _isPause = isPause;
        Apply();
    }

    /// <summary>source의 시간 배율을 설정(덮어쓰기)하고 timeScale을 재적용</summary>
    public void SetModifier(object source, float multiplier)
    {
        if (source == null)
        {
            Debug.LogWarning("TimeScaleManagerSO: source가 null입니다.");
            return;
        }

        modifiers[source] = Mathf.Max(0f, multiplier);
        Apply();
    }

    /// <summary>source의 시간 배율을 제거하고 timeScale을 재적용</summary>
    public void RemoveModifier(object source)
    {
        if (source == null)
            return;

        if (modifiers.Remove(source))
            Apply();
    }

    /// <summary>모든 배율과 일시정지 상태를 초기화하고 timeScale을 1로 되돌림</summary>
    public void ResetAll()
    {
        modifiers.Clear();
        _isPause = false;
        Apply();
    }

    /// <summary>배율을 모두 곱해 계산한 뒤 Time.timeScale에 적용</summary>
    private void Apply()
    {
        _scale = 1f;
        foreach (var multiplier in modifiers.Values)
            _scale *= multiplier;

        _modifierCount = modifiers.Count;
        Time.timeScale = _isPause ? 0f : Mathf.Min(_scale, 100f);
    }
}
