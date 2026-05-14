using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] PlayerDefaultStatDataSO playerDefaultStatData;
    [SerializeField] Transform playerWeaponPosition;
    [SerializeField] Transform Player;

    [SerializeField] float MoveSpeed;
    [SerializeField] float MoveAccel;


    [SerializeField] float rotateSpeed;
    [SerializeField] float rotateAccel;

    private void Start()
    {
        MoveSpeed = playerDefaultStatData.baseWeaponMoveSpeed;
        MoveAccel = playerDefaultStatData.baseWeaponMoveAccel;

        rotateSpeed = playerDefaultStatData.baseWeaponRotateSpeed;
        rotateAccel = playerDefaultStatData.baseWeaponRotateAccel;
    }

    private void Update()
    {
        AngleSet();
        MoveSet();
    }
    void AngleSet()
    {
        Vector3 vector = Player.transform.position - transform.position;
        float angle = Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg;
        float targetAngle = Mathf.LerpAngle(transform.eulerAngles.z, angle, rotateSpeed * rotateAccel * Time.deltaTime);
        Vector3 target = new Vector3(0, 0, targetAngle);
        transform.eulerAngles = target;
    }
    void MoveSet()
    {
        Vector3 target = playerWeaponPosition.position;
        transform.position = Vector3.Lerp(transform.position, target, MoveSpeed * MoveAccel * Time.deltaTime);
    }
}
