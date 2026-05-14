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
        FlatRadius, PercentRadius
    }

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

    public float FlatAD => flatAD;
    public float PercentAD => percentAD;
    public float FlatAS => flatAS;
    public float PercentAS => percentAS;
    public float FlatAngle => flatAngle;
    public float PercentAngle => percentAngle;
    public float FlatRadius => flatRadius;
    public float PercentRadius => percentRadius;

    public event Action OnWeaponStatChanged;

    [Button]
    public void AddUpgrade(WeaponStatType type, float value)
    {
        switch (type)
        {
            case WeaponStatType.FlatAD:      flatAD      += value; break;
            case WeaponStatType.PercentAD:   percentAD   += value; break;
            case WeaponStatType.FlatAS:      flatAS      += value; break;
            case WeaponStatType.PercentAS:   percentAS   += value; break;
            case WeaponStatType.FlatAngle:   flatAngle   += value; break;
            case WeaponStatType.PercentAngle:percentAngle+= value; break;
            case WeaponStatType.FlatRadius:  flatRadius  += value; break;
            case WeaponStatType.PercentRadius:percentRadius += value; break;
        }
        OnWeaponStatChanged?.Invoke();
    }
    public void ResetAllUpgrade()
    {
        flatAD = flatAS = percentAD = percentAS = 0;
        flatAngle = percentAngle = flatRadius = percentRadius = 0;
        OnWeaponStatChanged?.Invoke();
    }
}