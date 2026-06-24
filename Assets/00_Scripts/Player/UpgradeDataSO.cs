using System;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeDataSO", menuName = "Scriptable Objects/UpgradeDataSO")]
public class UpgradeDataSO : ScriptableObject
{
    public enum WeaponStatType
    {
        FlatAD, PercentAD,
        FlatAS, PercentAS,
        FlatAngle, PercentAngle,
        FlatRadius, PercentRadius,
        FlatBulletSpeed, PercentBulletSpeed,
    }

    [SerializeField] PlayerDefaultStatDataSO _playerDefaultStatData;
    [Title("Weapon")]
    //attack damage
    [SerializeField, ReadOnly] private float flatAD;
    [SerializeField, ReadOnly] private float percentAD;

    //attack speed
    [SerializeField, ReadOnly] private float flatAS;
    [SerializeField, ReadOnly] private float percentAS;

    //attack angle
    [SerializeField, ReadOnly] private float flatAngle;
    [SerializeField, ReadOnly] private float percentAngle;

    //attack radius
    [SerializeField, ReadOnly] private float flatRadius;
    [SerializeField, ReadOnly] private float percentRadius;

    [SerializeField, ReadOnly] private float flatBulletSpeed;
    [SerializeField, ReadOnly] private float percentBulletSpeed;

    public float FlatAD => flatAD;
    public float PercentAD => percentAD;
    public float FlatAS => flatAS;
    public float PercentAS => percentAS;
    public float FlatAngle => flatAngle;
    public float PercentAngle => percentAngle;
    public float FlatRadius => flatRadius;
    public float PercentRadius => percentRadius;
    public float FlatBulletSpeed => flatBulletSpeed;
    public float PercentBulletSpeed => percentBulletSpeed;

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
    public void AddUpgrade(WeaponStatType type, float value)
    {
        switch (type)
        {
            case WeaponStatType.FlatAD:             flatAD              += value; break;
            case WeaponStatType.PercentAD:          percentAD           += value; break;
            case WeaponStatType.FlatAS:             flatAS              += value; break;
            case WeaponStatType.PercentAS:          percentAS           += value; break;
            case WeaponStatType.FlatAngle:          flatAngle           += value; break;
            case WeaponStatType.PercentAngle:       percentAngle        += value; break;
            case WeaponStatType.FlatRadius:         flatRadius          += value; break;
            case WeaponStatType.PercentRadius:      percentRadius       += value; break;
            case WeaponStatType.FlatBulletSpeed:    flatBulletSpeed     += value; break;
            case WeaponStatType.PercentBulletSpeed: percentBulletSpeed  += value; break;
        }
        OnWeaponStatChanged?.Invoke();
    }
    public void ToUpgradeValue(WeaponStatType type, float value)
    {
        switch (type)
        {
            case WeaponStatType.FlatAD: flatAD = value; break;
            case WeaponStatType.PercentAD: percentAD = value; break;
            case WeaponStatType.FlatAS: flatAS = value; break;
            case WeaponStatType.PercentAS: percentAS = value; break;
            case WeaponStatType.FlatAngle: flatAngle = value; break;
            case WeaponStatType.PercentAngle: percentAngle = value; break;
            case WeaponStatType.FlatRadius: flatRadius = value; break;
            case WeaponStatType.PercentRadius: percentRadius = value; break;
            case WeaponStatType.FlatBulletSpeed: flatBulletSpeed = value; break;
            case WeaponStatType.PercentBulletSpeed: percentBulletSpeed = value; break;
        }
        OnWeaponStatChanged?.Invoke();
    }
    public void ResetAllUpgrade()
    {
        flatAD = flatAS = percentAD = percentAS = 0;
        flatAngle = percentAngle = flatRadius = percentRadius = 0;
        OnWeaponStatChanged?.Invoke();
    }

    public float CachedAttackDamage { get; private set; }
    public float CachedAttackSpeed { get; private set; }
    public float CachedSightAngle { get; private set; }
    public float CachedSightRadius { get; private set; }
    public float CachedBulletSpeed { get; private set; }
    private void CalculateStats()
    {
        CachedAttackDamage = (_playerDefaultStatData.baseWeaponAttackDamage + FlatAD)     * (1 + PercentAD);
        CachedAttackSpeed  = (_playerDefaultStatData.baseWeaponAttackSpeed  + FlatAS)     * (1 + PercentAS);
        CachedSightAngle   = (_playerDefaultStatData.baseAttackSightAngle   + FlatAngle)  * (1 + PercentAngle);
        CachedSightRadius  = (_playerDefaultStatData.baseAttackSightRadius  + FlatRadius) * (1 + PercentRadius);
        CachedAttackSpeed  = (_playerDefaultStatData.baseWeaponAttackSpeed  + FlatAS)     * (1 + PercentAS);
        CachedBulletSpeed  = (_playerDefaultStatData.baseBulletSpeed        + FlatAS)     * (1 +    PercentBulletSpeed);
    }
}