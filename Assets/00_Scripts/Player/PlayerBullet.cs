using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.Drawers;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;
public class PlayerBullet : MonoBehaviour
{
    private IObjectPool<PlayerBullet> pool;
    private Collider2D myCol;
    private float speed;
    private UpgradeDataSO upgradeDataSO;

    private CancellationTokenSource source;
    private CancellationToken token;

    /// <summary>타겟 위치로 발사하며 풀/업그레이드 데이터를 주입하고 이동 루프를 시작한다.</summary>
    public void Shoot(Vector2 target, float bulletSpeed, UpgradeDataSO upgradeData, IObjectPool<PlayerBullet> pool)
    {
        this.pool = pool;
        this.upgradeDataSO = upgradeData;

        Vector2 dir = target - (Vector2)transform.position;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
         speed = bulletSpeed;

        if(myCol is null)
            myCol = GetComponent<Collider2D>();

        source = new CancellationTokenSource();
        token = source.Token;
        moveBullet(token).Forget();
    }

    void Release()
    {
        pool.Release(this);
    }

    async UniTaskVoid moveBullet(CancellationToken token)
    {
        while (true)
        {
            transform.position += transform.up * speed;
            await UniTask.WaitForFixedUpdate(token);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision is null)
            return;

        if (collision.transform.CompareTag("Wall"))
        {
            source.Cancel();
            Release();
        }
        else if (collision.transform.TryGetComponent<IHitable>(out var hitable))
        {
            hitable.Hit(Mathf.CeilToInt(upgradeDataSO.CachedAttackDamage));
            source.Cancel();
            Release();
        }
    }
}