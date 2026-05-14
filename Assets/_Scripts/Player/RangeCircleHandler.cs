using Sirenix.OdinInspector;
using UnityEngine;

public class RangeCircleHandler : MonoBehaviour
{
    [SerializeField] private PlayerDefaultStatDataSO pcd;
    [SerializeField] private PolygonCollider2D triangle;
    [SerializeField] private CircleCollider2D circle;
    [SerializeField] private Transform circleSpriteObject;
    public float angle { get { return _angle; } set { _angle = value; updateRange(); } }
    public float radius { get { return _radius; } set { _radius = value; updateRange(); } }

    [SerializeField] Material _rangeMaterial;

    private float _angle;
    private float _radius;
    int angleId = Shader.PropertyToID("_Angle");

    private void Start()
    {
        _angle = pcd.baseAttackSightAngle;
        updateRange();
    }

    [Button]
    public void changeRange(float angle, float radius)
    {
        _angle = angle;
        _radius = radius;
        updateRange();
    }

    void updateRange()
    {
        _rangeMaterial.SetFloat(angleId, _angle);
        circle.radius = _radius;
        triangle.pathCount = 1;
        triangle.SetPath(0, DrawTriangle(_angle, _radius));
        circleSpriteObject.localScale = Vector3.one * radius * 2;

        transform.eulerAngles = new Vector3(0, 0, -angle / 2);
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
