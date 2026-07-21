using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

public class EnemySpawner : MonoBehaviour
{
    struct SpawnRect
    {
        public float left, right, top, bot;
        public float width => right - left;
        public float height => top - bot;
        public float Scale => Mathf.Max(0, width) * Mathf.Max(0, height);
    }

    private Dictionary<System.Type, IObjectPool<GameObject>> _poolDict = new();
    private Dictionary<System.Type, GameObject> _poolPrepab = new();
    private Dictionary<System.Type, GameObject> _enemyPrefabsByType = new();
    private string _currentBossKey;

    [SerializeField] private LevelManagerSO levelManager;
    [SerializeField] private Vector2 MapSize;
    [SerializeField] private Vector2 AroundPlayer;
    [SerializeField] private Transform player;

    private List<SpawnRect> rects = new();

    private async void Start()
    {
        await AddressableManager.Instance.RegistAsset("NormalEnemy");
        RebuildEnemyPrefabCache();

        var cancelToken = this.GetCancellationTokenOnDestroy();
        Spawner(cancelToken).Forget();

        levelManager.spawnBossEvent.AddListener(SpawnBoss);
        levelManager.spawnInfoUpdateEvent.AddListener(RegistEnemy);

        RegistEnemy();
    }
    private void OnDestroy()
    {
        levelManager.spawnBossEvent.RemoveListener(SpawnBoss);
        levelManager.spawnInfoUpdateEvent.RemoveListener(RegistEnemy);
    }

    /// <summary>AddressableManager에 로드된 프리팹들을 EnemyBase 타입 기준으로 캐싱합니다.</summary>
    private void RebuildEnemyPrefabCache()
    {
        _enemyPrefabsByType.Clear();
        foreach (var prefab in AddressableManager.Instance.Prefabs.Values)
        {
            EnemyBase enemyBase = prefab.GetComponent<EnemyBase>();
            if (enemyBase != null)
                _enemyPrefabsByType[enemyBase.GetType()] = prefab;
        }
    }

    private async UniTaskVoid Spawner(CancellationToken token)
    {
        while (true)
        {
            try
            {
                await UniTask.Delay(Mathf.FloorToInt(levelManager.enemySpawnDelay * 1000), cancellationToken: token);
                for (int i = 0; i < levelManager.numEnemySpawn; ++i)
                {
                    Spawn(levelManager.spawnInfo.SpawnTarget, SpawnablePoint());
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.ToString());
            }
            await UniTask.WaitForEndOfFrame(cancellationToken: token);
        }
    }

    /// <summary>현재 레벨의 EnemyWeights에 맞춰 풀 등록용 프리팹 목록을 다시 구성합니다.</summary>
    public void RegistEnemy()
    {
        ResetPool();
        foreach (var ew in levelManager.spawnInfo.EnemyWeights)
        {
            System.Type type = ew.targetType;
            if (type == null) continue;

            if (_enemyPrefabsByType.TryGetValue(type, out var prefab))
                RegisterPrefab(type, prefab);
            else
                Debug.LogError($"{type.Name}: AddressableManager에서 로드된 프리팹을 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 풀링 대상 타입에 사용할 프리팹을 등록합니다.
    /// </summary>
    public void RegisterPrefab(System.Type type, GameObject prefab)
    {
        _poolPrepab[type] = prefab;
    }

    public void ResetPool()
    {
        _poolPrepab = new();
    }

    public GameObject SpawnTarget(System.Type type)
    {
        if (!_poolDict.ContainsKey(type))
        {
            _poolDict.Add(type, initPool(type));
        }
        return _poolDict[type].Get();
    }

    /// <summary>
    /// type에 해당하는 적을 풀에서 꺼내 position 위치에 생성합니다.
    /// </summary>
    public GameObject Spawn(System.Type type, Vector2 position)
    {
        GameObject obj = SpawnTarget(type);
        obj.transform.position = position;

        EnemyBase enemy = obj.GetComponent<EnemyBase>();
        if (enemy != null)
            enemy.InitEnemy(this, player);

        return obj;
    }

    public void ReleaseObject(System.Type type, GameObject obj)
    {
        if (!_poolDict.ContainsKey(type))
            _poolDict.Add(type, initPool(type));

        _poolDict[type].Release(obj);
    }

    ObjectPool<GameObject> initPool(System.Type type)
    {
        return new ObjectPool<GameObject>(
                createFunc: () => Instantiate(_poolPrepab[type]),
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: obj => Destroy(obj),
                defaultCapacity: 5,
                maxSize: 300
                );
    }

    public Vector2 SpawnablePoint()
    {
        Vector2 center = player.position;
        SpawnRect mapRect = new SpawnRect
        {
            left = -MapSize.x / 2,
            right = MapSize.x / 2,
            top = MapSize.y / 2,
            bot = -MapSize.y / 2
        };

        SpawnRect playerRect = new SpawnRect
        {
            left = center.x - AroundPlayer.x / 2,
            right = center.x + AroundPlayer.x / 2,
            top = center.y + AroundPlayer.y / 2,
            bot = center.y - AroundPlayer.y / 2
        };
        rects.Clear();
        rects.Add(new SpawnRect
        {
            left = mapRect.left,
            right = mapRect.right,
            top = mapRect.top,
            bot = playerRect.top
        });

        rects.Add(new SpawnRect
        {
            left = mapRect.left,
            right = playerRect.left,
            top = playerRect.top,
            bot = playerRect.bot,
        });

        rects.Add(new SpawnRect
        {
            left = playerRect.right,
            right = mapRect.right,
            top = playerRect.top,
            bot = playerRect.bot
        });

        rects.Add(new SpawnRect
        {
            left = mapRect.left,
            right = mapRect.right,
            top = playerRect.bot,
            bot = mapRect.bot
        });

        SpawnRect chosen = default;
        bool found = false;
        float runningTotal = 0;
        for(int i = 0; i < rects.Count; ++i)
        {
            runningTotal += rects[i].Scale;
            if (Random.Range(0f, runningTotal) < rects[i].Scale)
            {
                chosen = rects[i];
                found = true;
            }
        }
        if (!found)
            return new Vector2(Random.Range(mapRect.left, mapRect.right), Random.Range(mapRect.bot, mapRect.top));

        return new Vector2(Random.Range(chosen.left, chosen.right), Random.Range(chosen.bot, chosen.top));
    }
    /// <summary>보스 키에 해당하는 프리팹을 로드해 스폰하고, 이전 보스 에셋은 해제합니다.</summary>
    public async void SpawnBoss(string bossKey)
    {
        if (string.IsNullOrEmpty(bossKey))
        {
            Debug.Log($"{levelManager.level}에 Boss Object가 없어요");
            return;
        }

        if (!string.IsNullOrEmpty(_currentBossKey) && _currentBossKey != bossKey)
            AddressableManager.Instance.ReleaseKey(_currentBossKey);

        await AddressableManager.Instance.RegistAssetByKey(bossKey);
        _currentBossKey = bossKey;

        GameObject bossObject = AddressableManager.Instance.GetPrefab(bossKey);
        if (bossObject == null)
        {
            Debug.LogError($"{bossKey}: 보스 프리팹을 로드하지 못했습니다.");
            return;
        }

        Vector2 pos = SpawnablePoint();
        Instantiate(bossObject, pos, Quaternion.identity);
        Debug.Log("Boss is comming!!!");
    }
}
