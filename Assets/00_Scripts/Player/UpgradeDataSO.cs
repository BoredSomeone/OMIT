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

    [Title("Item")]
    [ReadOnly] private Dictionary<StatType, float> itemFlat = new();
    [ReadOnly] private Dictionary<StatType, float> itemPercent = new();

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

    /// <summary>지정한 타입에 업그레이드 값을 더하고 능력치 재계산</summary>
    [Button]
    public void AddUpgrade(StatType type, bool isPercent, float value)
    {
        var dict = isPercent ? percent : flat;
        dict[type] = dict.GetValueOrDefault(type) + value;
        OnWeaponStatChanged?.Invoke();
    }

    /// <summary>지정한 타입의 업그레이드 값을 지정한 값으로 덮어쓰고 재계산</summary>
    public void ToUpgradeValue(StatType type, bool isPercent, float value)
    {
        var dict = isPercent ? percent : flat;
        dict[type] = value;
        OnWeaponStatChanged?.Invoke();
    }

    /// <summary>모든 flat/percent 업그레이드 값을 초기화하고 재계산</summary>
    public void ResetAllUpgrade()
    {
        flat.Clear();
        percent.Clear();
        OnWeaponStatChanged?.Invoke();
    }

    /// <summary>보유 아이템 스탯 합계를 지정한 값으로 덮어쓰고 재계산</summary>
    public void SetItemStats(IReadOnlyDictionary<StatType, float> flatSum, IReadOnlyDictionary<StatType, float> percentSum)
    {
        itemFlat.Clear();
        itemPercent.Clear();
        foreach (var (type, value) in flatSum)
            itemFlat[type] = value;
        foreach (var (type, value) in percentSum)
            itemPercent[type] = value;
        OnWeaponStatChanged?.Invoke();
    }

    /// <summary>업그레이드가 반영된 최종 스탯 값을 반환</summary>
    public float GetCached(StatType type) => cached.GetValueOrDefault(type);

    /// <summary>percent 적용 전 합계(기본 + 업그레이드 flat + 아이템 flat)를 반환</summary>
    public float GetFlatTotal(StatType type)
    {
        float baseValue = _playerDefaultStatData != null && baseGetters.TryGetValue(type, out var getBase)
            ? getBase(_playerDefaultStatData)
            : 0f;
        return baseValue + flat.GetValueOrDefault(type) + itemFlat.GetValueOrDefault(type);
    }

    private void CalculateStats()
    {
        foreach (var (type, getBase) in baseGetters)
        {
            float baseValue = getBase(_playerDefaultStatData);

            cached[type] = (baseValue + flat.GetValueOrDefault(type) + itemFlat.GetValueOrDefault(type))
                           * (1 + percent.GetValueOrDefault(type) + itemPercent.GetValueOrDefault(type));
        }
    }
}
