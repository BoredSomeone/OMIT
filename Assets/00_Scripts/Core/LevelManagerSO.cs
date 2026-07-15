using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelManagerSO", menuName = "Scriptable Objects/LevelManager")]
public class LevelManagerSO : ScriptableObject
{
    [SerializeField] private int _killsToLevelUp;

    [SerializeField, ReadOnly] private int _level = 1;
    [SerializeField, ReadOnly] private int _baseHP;

    public int baseHP
    {
        get
        {
            return _baseHP;
        }
    }

    private void OnEnable()
    {
        /// 나중에 저장/로드 제작하면 시작값 변경 필요함
        _level = 0;
        LevelUp();
    }

    public int LevelUp()
    {
        ++_level;
        _baseHP = Mathf.FloorToInt(100 * (1 + _level * 0.05f) * (1 + (_level / 5) * 0.3f));
        return _level;
    }
}
