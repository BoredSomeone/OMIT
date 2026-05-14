using System.Collections;
using UnityEngine;

public class WeaponHandler : MonoBehaviour
{
    [SerializeField] PlayerDefaultStatDataSO _playerDefaultStatData;
    [SerializeField] UpgradeDataSO _upgradeData;
    [SerializeField] private float _attackPerSec;
    [SerializeField] private RangeCircleHandler _rangeCircleHandler;

    private float _baseWeaponSpeed;

    private Coroutine attackCoroutine;
    private Coroutine moveCoroutine;

    public float CachedAttackDamage { get; private set; }
    public float CachedAttackSpeed { get; private set; }
    public float CachedSightAngle { get; private set; }
    public float CachedSightRadius { get; private set; }

    private void OnEnable()
    {
        _upgradeData.OnWeaponStatChanged += CalculateStats;
    }

    private void OnDisable()
    {
        _upgradeData.OnWeaponStatChanged -= CalculateStats;
    }

    private void Start()
    {
        if (_attackPerSec <= 0)
            Debug.LogWarning("_attackPerSec is 0!!!");

        _baseWeaponSpeed = _playerDefaultStatData.baseWeaponMoveSpeed;
        CalculateStats();
    }

    private void CalculateStats()
    {
        CachedAttackDamage = (_playerDefaultStatData.baseWeaponAttackDamage + _upgradeData.FlatAD) * (1 + _upgradeData.PercentAD);
        CachedAttackSpeed  = (_playerDefaultStatData.baseWeaponAttackSpeed  + _upgradeData.FlatAS) * (1 + _upgradeData.PercentAS);
        CachedSightAngle   = (_playerDefaultStatData.baseAttackSightAngle   + _upgradeData.FlatAngle)  * (1 + _upgradeData.PercentAngle);
        CachedSightRadius  = (_playerDefaultStatData.baseAttackSightRadius  + _upgradeData.FlatRadius) * (1 + _upgradeData.PercentRadius);

        _rangeCircleHandler.angle  = CachedSightAngle;
        _rangeCircleHandler.radius = CachedSightRadius;
    }

    IEnumerator Attack()
    {
        while (true)
        {
            yield return new WaitForSeconds(1 / _attackPerSec);
        }
    }

    void targetting()
    {

    }
}
