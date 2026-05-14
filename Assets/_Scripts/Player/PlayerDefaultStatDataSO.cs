using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerDefaultStatDataSO", menuName = "Scriptable Objects/PlayerDefaultStatDataSO")]
public class PlayerDefaultStatDataSO : ScriptableObject
{
    [Title("move")]
    [SerializeField] private float _baseMaxSpeed;
    [SerializeField] private float _baseAcceleration;
    [SerializeField] private float _baseBreakDamping;

    [Title("weapon")]
    [SerializeField] private float _baseWeaponAttackDamage;
    [SerializeField] private float _baseWeaponAttackSpeed;

    [Space]
    [SerializeField] private float _baseWeaponMoveSpeed;
    [SerializeField] private float _baseWeaponMoveAccel;
    [SerializeField] private float _baseWeaponRotateSpeed;
    [SerializeField] private float _baseWeaponRotateAccel;
    [Space]
    [SerializeField] private float _baseAttackSightAngle;
    [SerializeField] private float _baseAttackSightRadius;

    public float baseMaxSpeed => _baseMaxSpeed;
    public float baseAcceleration => _baseAcceleration;
    public float baseBreakDamping => _baseBreakDamping;
    public float baseWeaponAttackDamage => _baseWeaponAttackDamage;
    public float baseWeaponAttackSpeed => _baseWeaponAttackSpeed;
    public float baseWeaponMoveSpeed => _baseWeaponMoveSpeed;
    public float baseWeaponMoveAccel => _baseWeaponMoveAccel;
    public float baseWeaponRotateSpeed => _baseWeaponRotateSpeed;
    public float baseWeaponRotateAccel => _baseWeaponRotateAccel;
    public float baseAttackSightAngle => _baseAttackSightAngle;
    public float baseAttackSightRadius => _baseAttackSightRadius;
}
