using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] UpgradeDataSO upgradeData;
    [SerializeField] Transform playerWeaponPosition;
    [SerializeField] Transform Player;

    private void FixedUpdate()
    {
        AngleSet();
        MoveSet();
    }
    void AngleSet()
    {
        Vector3 vector = Player.transform.position - transform.position;
        float angle = Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
        float rotateSpeed = upgradeData.GetCached(UpgradeDataSO.StatType.WeaponRotateSpeed);
        float rotateAccel = upgradeData.GetCached(UpgradeDataSO.StatType.WeaponRotateAccel);
        float targetAngle = Mathf.LerpAngle(transform.eulerAngles.z, angle, rotateSpeed * rotateAccel * Time.fixedDeltaTime);
        Vector3 target = new Vector3(0, 0, targetAngle);
        transform.eulerAngles = target;
    }
    void MoveSet()
    {
        Vector3 target = playerWeaponPosition.position;
        float moveSpeed = upgradeData.GetCached(UpgradeDataSO.StatType.WeaponMoveSpeed);
        float moveAccel = upgradeData.GetCached(UpgradeDataSO.StatType.WeaponMoveAccel);
        transform.position = Vector3.Lerp(transform.position, target, moveSpeed * moveAccel * Time.fixedDeltaTime);
    }
}
