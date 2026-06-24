using Sirenix.OdinInspector;
using UnityEngine;

public class RangeCircleHandler : MonoBehaviour
{
    [SerializeField] private PlayerDefaultStatDataSO pcd;
    [SerializeField] private UpgradeDataSO upgradeData;
    [SerializeField] private CompositeCollider2D attackRange;
    [SerializeField] private PolygonCollider2D triangle;
    [SerializeField] private CircleCollider2D circle;
    [SerializeField] private Transform circleSpriteObject;
    public Collider2D GetAttackCollider { get { return attackRange; } }

    [SerializeField] Material _rangeMaterial;
    [Space]
    [SerializeField] private float angle;
    [SerializeField] private float radius;
    int angleId = Shader.PropertyToID("_Angle");

    private void Start()
    {
        angle = upgradeData.CachedSightAngle;
        radius = upgradeData.CachedSightRadius;
        upgradeData.OnWeaponStatChanged += UpdateRange;
        UpdateRange();
    }

    [Button]
    public void ChangeRange(float angle, float radius)
    {
        upgradeData.ToUpgradeValue(UpgradeDataSO.WeaponStatType.FlatAngle, angle);
        upgradeData.ToUpgradeValue(UpgradeDataSO.WeaponStatType.FlatRadius, radius);
    }

    void UpdateRange()
    {
        _rangeMaterial.SetFloat(angleId, angle);
        circle.radius = radius;
        triangle.pathCount = 1;
        triangle.SetPath(0, DrawTriangle(angle, radius));
        circleSpriteObject.localScale = Vector3.one * radius * 2;

        transform.localEulerAngles = new Vector3(0, 0, -angle / 2);
    }

    Vector2[] DrawTriangle(float angle, float radius)
    {
        int sign = 1;
        angle %= 360;
        if(angle > 180)
        {
            angle = 360 - angle;
            sign = -1;
        }
        float rad = angle * Mathf.Deg2Rad;

        Vector2[] points = new Vector2[3];
        points[0] = Vector2.zero;
        points[1] = new Vector2(-radius / Mathf.Cos(rad / 2f), 0);
        points[2] = sign * new Vector2(-Mathf.Cos(rad), -Mathf.Sin(rad)) * radius / Mathf.Cos(rad / 2f);
        return points;
    }
}
