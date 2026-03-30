using UnityEngine;

[CreateAssetMenu(fileName = "PlayerControlDataSO", menuName = "Scriptable Objects/PlayerControlDataSO")]
public class PlayerControlDataSO : ScriptableObject
{
    public float baseMaxSpeed;
    public float baseAcceleration;
    public float baseBreakDamping;

    public float baseWeaponSpeed;
}
