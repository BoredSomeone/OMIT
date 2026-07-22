using System.Collections;
using System.Threading;
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
    [SerializeField] protected int exp;

    protected LevelManagerSO levelManager;
    private EnemySpawner _spawner;
    private CancellationTokenSource _spawnCts;

    public float getMoveSpeed => moveSpeedBase * moveSpeedRatio;

    public int maxHP => _maxHP;
    public int nowHP => _nowHP;

    public Collider2D getCollider => _myCol;
    public Vector2 GetTargetPoint => (Vector2)transform.position + TargetOffset;

    protected CancellationToken spawnToken => _spawnCts.Token;

    private Vector3 farPosition = Vector3.one * 50000;

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
    /// 풀에서 꺼내져 스폰될 때 스포너가 호출합니다. 위치 세팅과 스탯/상태 초기화, 스폰 시점 취소 토큰 갱신을 모두 담당합니다.
    /// </summary>
    public virtual void InitEnemy(EnemySpawner spawner, Transform player, Vector2 position, LevelManagerSO levelManager)
    {
        transform.position = position;

        _spawner = spawner;
        this.player = player;
        this.levelManager = levelManager;

        _spawnCts?.Cancel();
        _spawnCts?.Dispose();
        _spawnCts = new CancellationTokenSource();

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
        OnStart();
    }

    protected virtual void OnStart()
    {

    }

    protected virtual void OnDisable()
    {
        transform.position = farPosition;
        _spawnCts?.Cancel();
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