using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;
public class PlayerBullet : MonoBehaviour
{
    private IObjectPool<PlayerBullet> pool;
    private float speed;
    private UpgradeDataSO upgradeDataSO;
    private bool isReleased;

    private void Awake()
    {
        if (!TryGetComponent<Collider2D>(out _))
            Debug.LogError($"{name}: 총알 프리팹에 Collider2D가 필요합니다.");
    }

    public void Shoot(Vector2 target, float bulletSpeed, UpgradeDataSO upgradeData, IObjectPool<PlayerBullet> pool)
    {
        this.pool = pool;
        this.upgradeDataSO = upgradeData;
        isReleased = false;

        Vector2 dir = target - (Vector2)transform.position;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        speed = bulletSpeed;
    }

    void Release()
    {
        if (isReleased)
            return;

        isReleased = true;
        pool.Release(this);
    }

    private void FixedUpdate()
    {
        transform.position += transform.up * speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision is null)
            return;

        if (collision.transform.CompareTag("Wall"))
        {
            Release();
        }
        else if (collision.transform.TryGetComponent<IHitable>(out var hitable))
        {
            hitable.Hit(Mathf.CeilToInt(upgradeDataSO.GetCached(UpgradeDataSO.StatType.AttackDamage)));
            Release();
        }
    }
}