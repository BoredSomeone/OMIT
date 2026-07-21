using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

public class WeaponHandler : MonoBehaviour
{
    [SerializeField] UpgradeDataSO _upgradeData;
    [SerializeField] private RangeCircleHandler _rangeCircleHandler;
    [SerializeField] Transform player;

    [SerializeField] private ObjectPool<PlayerBullet> _bulletPool;
    [SerializeField] private GameObject bulletPrefab;
    List<Collider2D> results = new();


    private void Start()
    {
        var cancelToken = this.GetCancellationTokenOnDestroy();
        FindTarget(cancelToken).Forget();

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
            try
            {
                float attackSpeed = _upgradeData.GetCached(UpgradeDataSO.StatType.AttackSpeed);
                if (TryFindAttackTarget(out var target) && attackSpeed > 0)
                {
                    Shoot(target);
                    await UniTask.Delay((int)(1000f / attackSpeed), DelayType.DeltaTime, cancellationToken: token);
                }
            }
            catch (System.Exception e) when (e is not OperationCanceledException)
            {
                Debug.LogError(e);
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

        bullet.Shoot(target.GetTargetPoint, _upgradeData.GetCached(UpgradeDataSO.StatType.BulletSpeed), _upgradeData, _bulletPool);
    }


}
