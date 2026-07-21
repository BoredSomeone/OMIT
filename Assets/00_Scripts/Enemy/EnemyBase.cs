using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class EnemyBase : MonoBehaviour, IHitable, IEnemyStats
{
    [SerializeField] private Collider2D _myCol;
    [SerializeField] private Slider hpBar;
    [SerializeField] private float hpScale = 1;
    [SerializeField] private int _maxHP = 1;
    [SerializeField] private int _nowHP = 0;
    [SerializeField] protected EnemyExpTableSO enemyExpTable;
    [SerializeField] protected Transform player;

    [SerializeField] private float moveSpeedBase = 1;
    [SerializeField] private float moveSpeedRatio = 1;

    [SerializeField] Vector2 TargetOffset;
    [SerializeField] protected LevelManagerSO levelManager;
    [SerializeField] protected int exp;

    private EnemySpawner _spawner;

    public float getMoveSpeed => moveSpeedBase * moveSpeedRatio;

    public int maxHP => _maxHP;
    public int nowHP => _nowHP;

    public Collider2D getCollider => _myCol;
    public Vector2 GetTargetPoint => (Vector2)transform.position + TargetOffset;

    protected virtual void Start()
    {
    }

    protected virtual void InitExp()
    {
        exp = enemyExpTable.GetEXP(typeof(EnemyBase));
    }

    public void MoveSpeedRatioChange(float speedRatio)
    {
        moveSpeedRatio += speedRatio;
        if (moveSpeedRatio <= 0.1f)
            moveSpeedRatio = 0.1f;
    }

    /// <summary>
    /// 이 적을 생성/회수할 EnemySpawner를 등록합니다. 사망 시 풀로 반환하기 위해 사용됩니다.
    /// </summary>
    public void InitEnemy(EnemySpawner spawner, Transform player)
    {
        _spawner = spawner;
        this.player = player;

        _maxHP = Mathf.FloorToInt(levelManager.baseHP * hpScale);
        _nowHP = _maxHP;
        InitExp();

        if (_myCol is null)
            _myCol = GetComponent<Collider2D>();

        if (_myCol == null || _myCol.attachedRigidbody == null)
            Debug.LogError($"{name}: Collider2D 또는 Rigidbody2D가 없습니다.");

        if (hpBar)
        {
            hpBar.maxValue = _maxHP;
            hpBar.value = _nowHP;
            hpBar.minValue = 0;
        }
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

        levelManager.AddEXP(exp);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(GetTargetPoint, 0.1f);
    }
#endif
}