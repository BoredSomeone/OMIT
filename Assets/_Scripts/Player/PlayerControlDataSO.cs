using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerControlDataSO", menuName = "Scriptable Objects/PlayerControlDataSO")]
public class PlayerControlDataSO : ScriptableObject
{
    [Title("move")]
    public float baseMaxSpeed { get; private set; }
    public float baseAcceleration { get; private set; }
    public float baseBreakDamping { get; private set; }

    [Title("weapon")]
    public float baseWeaponSpeed { get; private set; }
    public float baseAttackAngle { get; private set; }
}
