using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeDataSO", menuName = "Scriptable Objects/UpgradeDataSO")]
public class UpgradeDataSO : SerializedScriptableObject
{
    public enum StatType
    {
        AttackDamage,
        AttackSpeed,
        SightAngle,
        SightRadius,
        BulletSpeed,
        MoveSpeed,
        WeaponMoveSpeed,
        WeaponMoveAccel,
        WeaponRotateSpeed,
        WeaponRotateAccel,
    }

    [SerializeField] PlayerDefaultStatDataSO _playerDefaultStatData;

    [Title("Upgrade")]
    [ReadOnly] private Dictionary<StatType, float> flat = new();
    [ReadOnly] private Dictionary<StatType, float> percent = new();
    [ReadOnly] private Dictionary<StatType, float> cached = new();

    static readonly Dictionary<StatType, Func<PlayerDefaultStatDataSO, float>> baseGetters = new()
    {
        { StatType.AttackDamage,      d => d.baseWeaponAttackDamage },
        { StatType.AttackSpeed,       d => d.baseWeaponAttackSpeed },
        { StatType.SightAngle,        d => d.baseAttackSightAngle },
        { StatType.SightRadius,       d => d.baseAttackSightRadius },
        { StatType.BulletSpeed,       d => d.baseBulletSpeed },
        { StatType.MoveSpeed,         d => d.baseMaxSpeed },
        { StatType.WeaponMoveSpeed,   d => d.baseWeaponMoveSpeed },
        { StatType.WeaponMoveAccel,   d => d.baseWeaponMoveAccel },
        { StatType.WeaponRotateSpeed, d => d.baseWeaponRotateSpeed },
        { StatType.WeaponRotateAccel, d => d.baseWeaponRotateAccel },
    };

    public event Action OnWeaponStatChanged;

    private void OnEnable()
    {
        OnWeaponStatChanged += CalculateStats;
        CalculateStats();
    }

    private void OnDisable()
    {
        OnWeaponStatChanged -= CalculateStats;
    }

    [Button]
    public void AddUpgrade(StatType type, bool isPercent, float value)
    {
        var dict = isPercent ? percent : flat;
        dict[type] = dict.GetValueOrDefault(type) + value;
        OnWeaponStatChanged?.Invoke();
    }

    public void ToUpgradeValue(StatType type, bool isPercent, float value)
    {
        var dict = isPercent ? percent : flat;
        dict[type] = value;
        OnWeaponStatChanged?.Invoke();
    }

    public void ResetAllUpgrade()
    {
        flat.Clear();
        percent.Clear();
        OnWeaponStatChanged?.Invoke();
    }

    /// <summary>업그레이드가 반영된 최종 스탯 값을 반환한다.</summary>
    public float GetCached(StatType type) => cached.GetValueOrDefault(type);

    private void CalculateStats()
    {
        foreach (var (type, getBase) in baseGetters)
        {
            float baseValue = getBase(_playerDefaultStatData);
            cached[type] = (baseValue + flat.GetValueOrDefault(type)) * (1 + percent.GetValueOrDefault(type));
        }
    }
}
