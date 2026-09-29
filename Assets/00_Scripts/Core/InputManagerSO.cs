using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 액션 맵 전환을 담당하는 유일한 관리자.
/// 하나의 InputActionAsset을 공유하며, 상황에 맞는 액션 맵만 활성화한다.
/// </summary>
[CreateAssetMenu(fileName = "InputManagerSO", menuName = "Scriptable Objects/InputManagerSO")]
public class InputManagerSO : ScriptableObject
{
    public enum InputMapType
    {
        Player,
        UI,
    }

    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField, ReadOnly] private InputMapType _nowMap;

    public InputMapType nowMap { get { return _nowMap; } }

    /// <summary>"맵/액션" 경로로 액션을 찾아 반환</summary>
    public InputAction FindAction(string actionPath)
    {
        return actionAsset.FindAction(actionPath, true);
    }

    /// <summary>지정한 액션 맵만 활성화하고 나머지 맵은 비활성화</summary>
    public void SwitchMap(InputMapType mapType)
    {
        string mapName = mapType.ToString();
        foreach (var map in actionAsset.actionMaps)
        {
            if (map.name != mapName)
                map.Disable();
        }

        actionAsset.FindActionMap(mapName, true).Enable();
        _nowMap = mapType;
    }
}
