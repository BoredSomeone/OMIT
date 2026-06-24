using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class EnemyBase : MonoBehaviour, IHitable, EnemyDatas
{
    [SerializeField] private Collider2D _myCol;
    [SerializeField] private Slider hpBar;
    [SerializeField] private int _maxHP = 1;
    [SerializeField] private int _nowHP = 0;

    [SerializeField] private float moveSpeedBase = 1;
    [SerializeField] private float moveSpeedRatio = 1;
    private EnemySpawner _spawner;

    [SerializeField] Vector2 TargetOffset;

    public float getMoveSpeed => moveSpeedBase * moveSpeedRatio;

    public int maxHP => _maxHP;
    public int nowHP => _nowHP;

    public Collider2D getCollider => _myCol;
    public Vector2 GetTargetPoint => (Vector2)transform.position + TargetOffset;

    private void Start()
    {
        if (_myCol is null)
            _myCol = GetComponent<Collider2D>();

        _nowHP = _maxHP;
        if (hpBar)
        {
            hpBar.maxValue = _maxHP;
            hpBar.value = _nowHP;
            hpBar.minValue = 0;
        }
    }

    public void moveSpeedRatioChange(float speedRatio)
    {
        moveSpeedRatio += speedRatio;
        if (moveSpeedRatio <= 0.1f)
            moveSpeedRatio = 0.1f;
    }

    /// <summary>
    /// 이 적을 생성/회수할 EnemySpawner를 등록합니다. 사망 시 풀로 반환하기 위해 사용됩니다.
    /// </summary>
    public void SetSpawner(EnemySpawner spawner)
    {
        _spawner = spawner;
    }

    public void Hit(int damage)
    {
        _nowHP -= damage;
        if (hpBar)
            hpBar.value = _nowHP;
        if (_nowHP <= 0)
            Dead();
    }

    private void Dead()
    {
        if (_spawner != null)
            _spawner.ReleaseObject(GetType(), gameObject);
        else
            Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(GetTargetPoint, 0.1f);
    }
#endif
}