using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

[SelectionBase]
public class Enemy_AluminumCan : EnemyBase
{
    [SerializeField] float speed;
    [SerializeField] float dashInterval;

    protected override void Start()
    {
        base.Start();

        var cancelToken = this.GetCancellationTokenOnDestroy();
        Move(cancelToken).Forget();
    }

    protected override void InitExp()
    {
        exp = enemyExpTable.GetEXP(typeof(Enemy_AluminumCan));
    }

    async UniTaskVoid Move(CancellationToken token)
    {
        while (true)
        {
            var delay = Mathf.FloorToInt(dashInterval * 1000);
            Vector3 playerP;

            if (player)
                playerP = player.position;
            else
            {
                playerP = transform.position;
                playerP += new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);
                Debug.LogError($"{this.name} Player is null");
            }

                getCollider.attachedRigidbody.linearVelocity = Vector3.zero;
            await UniTask.Delay(300, cancellationToken: token);

            var dir = ((Vector2)(playerP - transform.position)).normalized;
            getCollider.attachedRigidbody.AddForce(dir * speed, ForceMode2D.Impulse);
            await UniTask.Delay(Mathf.Max(0, delay - 300), cancellationToken: token);
        }
    }
}
