using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

public class WeaponHandler : MonoBehaviour
{
    [SerializeField] PlayerDefaultStatDataSO _playerDefaultStatData;
    [SerializeField] UpgradeDataSO _upgradeData;
    [SerializeField] private RangeCircleHandler _rangeCircleHandler;
    [SerializeField] Transform player;

    [SerializeField] private ObjectPool<PlayerBullet> _bulletPool;
    [SerializeField] private GameObject bulletPrefab;
    List<Collider2D> results = new();


    private void Start()
    {
        var cancleToken = this.GetCancellationTokenOnDestroy();
        FindTarget(cancleToken).Forget();

        _bulletPool = new ObjectPool<PlayerBullet>(
            createFunc: () => Instantiate(bulletPrefab).GetComponent<PlayerBullet>(),
            actionOnGet: b =>
            {
                b.transform.position = transform.position;
                b.gameObject.SetActive(true);
            },
            actionOnRelease: b => b.gameObject.SetActive(false),
            actionOnDestroy: b => Destroy(b.gameObject),
            defaultCapacity: 30,
            maxSize: 500
            );
    }

    private async UniTaskVoid FindTarget(CancellationToken token)
    {
        while (true)
        {
            if (TryFindAttackTarget(out var target))
            {
                Shoot(target);
                await UniTask.Delay((int)(1000f / _upgradeData.CachedAttackSpeed), DelayType.DeltaTime, cancellationToken: token);
            }
            await UniTask.WaitForEndOfFrame(token);
        }
    }

    bool TryFindAttackTarget(out IHitable target)
    {
        target = null;
        var range = _rangeCircleHandler.GetAttackCollider;
        int count = range.Overlap(results);

        float closestDistSqr = float.MaxValue;
        Vector2 origin = player.position;
        for (int i = 0; i < count; ++i)
        {
            var col = results[i];
            if(col.TryGetComponent<IHitable>(out var hitable))
            {
                float distSqr = (origin - col.ClosestPoint(origin)).sqrMagnitude;
                if (distSqr < closestDistSqr)
                {
                    closestDistSqr = distSqr;
                    target = hitable;
                }
            }
        }
        return target != null;
    }

    void Shoot(IHitable target)
    {
        var bullet = _bulletPool.Get();

        bullet.Shoot(target.GetTargetPoint, _upgradeData.CachedBulletSpeed, _upgradeData, _bulletPool);
    }


}
