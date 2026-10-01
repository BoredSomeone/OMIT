using Sirenix.OdinInspector;
using UnityEngine;

public class RangeCircleHandler : MonoBehaviour
{
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
        if (!attackRange || !triangle || !circle || !circleSpriteObject || !_rangeMaterial)
        {
            Debug.LogError($"{name}: RangeCircleHandler에 필요한 참조가 누락되었습니다.");
            return;
        }

        upgradeData.OnWeaponStatChanged += OnStatChanged;
        OnStatChanged();
    }

    private void OnDestroy()
    {
        if (upgradeData != null)
            upgradeData.OnWeaponStatChanged -= OnStatChanged;
    }

    /// <summary>UpgradeDataSO의 최종 시야 수치를 다시 읽어 범위를 갱신</summary>
    private void OnStatChanged()
    {
        angle = upgradeData.GetCached(UpgradeDataSO.StatType.SightAngle);
        radius = upgradeData.GetCached(UpgradeDataSO.StatType.SightRadius);
        UpdateRange();
    }

    [Button]
    private void ChangeRange(float angle, float radius)
    {
        upgradeData.ToUpgradeValue(UpgradeDataSO.StatType.SightAngle, false, angle);
        upgradeData.ToUpgradeValue(UpgradeDataSO.StatType.SightRadius, false, radius);
    }

    void UpdateRange()
    {
        angle = Mathf.Clamp(angle, 0, 360);
        _rangeMaterial.SetFloat(angleId, angle);

        circle.radius = radius;
        transform.localEulerAngles = new Vector3(0, 0, -angle / 2);
        triangle.pathCount = 1;
        circleSpriteObject.localScale = Vector3.one * radius * 2;
        transform.localEulerAngles = new Vector3(0, 0, -angle / 2);

        if (angle <= 180)
        {
            triangle.SetPath(0, DrawTriangle(angle, radius));
            triangle.compositeOperation = Collider2D.CompositeOperation.Intersect;
            triangle.transform.localRotation = Quaternion.identity;
        }
        else
        {
            triangle.SetPath(0, DrawTriangle(360 - angle, radius));
            triangle.compositeOperation = Collider2D.CompositeOperation.Difference;
            triangle.transform.localRotation = Quaternion.Euler(0, 0, angle - 360);
        }
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateRange();
    }
#endif
}
