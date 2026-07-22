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
    private Dictionary<System.Type, GameObject> _poolPrefab = new();
    private Dictionary<System.Type, GameObject> _enemyPrefabsByType = new();
    private string _currentBossKey;

    [SerializeField] private LevelManagerSO levelManager;
    [SerializeField] private Vector2 mapSize;
    [SerializeField] private Vector2 aroundPlayer;
    [SerializeField] private Transform player;

    private List<SpawnRect> rects = new();

    private async void Start()
    {
        await AddressableManager.Instance.RegisterAsset("NormalEnemy");
        RebuildEnemyPrefabCache();

        var cancelToken = this.GetCancellationTokenOnDestroy();
        Spawner(cancelToken).Forget();

        levelManager.spawnBossEvent.AddListener(SpawnBoss);
        levelManager.spawnInfoUpdateEvent.AddListener(RegisterEnemy);

        RegisterEnemy();
    }
    private void OnDestroy()
    {
        levelManager.spawnBossEvent.RemoveListener(SpawnBoss);
        levelManager.spawnInfoUpdateEvent.RemoveListener(RegisterEnemy);
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
            catch (System.Exception e) when (e is not System.OperationCanceledException)
            {
                if (levelManager.spawnInfo.SpawnTarget is null)
                    Debug.LogError($"{levelManager.level}에 유효한 EnemyWeights가 없습니다.");
                Debug.LogError(e.ToString());
            }
            await UniTask.WaitForEndOfFrame(cancellationToken: token);
        }
    }

    /// <summary>현재 레벨의 EnemyWeights에 맞춰 풀 등록용 프리팹 목록을 다시 구성합니다.</summary>
    public void RegisterEnemy()
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
        _poolPrefab[type] = prefab;
    }

    public void ResetPool()
    {
        _poolPrefab = new();
    }

    /// <summary>
    /// type에 해당하는 오브젝트를 풀에서 꺼냅니다.
    /// </summary>
    public GameObject GetFromPool(System.Type type)
    {
        return GetOrCreatePool(type).Get();
    }

    /// <summary>
    /// type에 해당하는 적을 풀에서 꺼내 position 위치에 스폰합니다.
    /// </summary>
    public GameObject Spawn(System.Type type, Vector2 position)
    {
        GameObject obj = GetFromPool(type);

        if (obj.TryGetComponent<EnemyBase>(out var enemy))
            enemy.InitEnemy(this, player, position);

        return obj;
    }

    public void ReleaseObject(System.Type type, GameObject obj)
    {
        GetOrCreatePool(type).Release(obj);
    }

    private IObjectPool<GameObject> GetOrCreatePool(System.Type type)
    {
        if (!_poolDict.TryGetValue(type, out var pool))
        {
            pool = InitPool(type);
            _poolDict.Add(type, pool);
        }
        return pool;
    }

    /// <summary>
    /// type에 해당하는 오브젝트 풀을 생성합니다.
    /// </summary>
    ObjectPool<GameObject> InitPool(System.Type type)
    {
        return new ObjectPool<GameObject>(
                createFunc: () => Instantiate(_poolPrefab[type]),
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: obj => Destroy(obj),
                defaultCapacity: 20,
                maxSize: 300
                );
    }

    /// <summary>
    /// 플레이어 주변에서 떨어져서 적을 스폰합니다
    /// </summary>
    /// <returns></returns>
    public Vector2 SpawnablePoint()
    {
        Vector2 center = player.position;
        SpawnRect mapRect = new SpawnRect
        {
            left = -mapSize.x / 2,
            right = mapSize.x / 2,
            top = mapSize.y / 2,
            bot = -mapSize.y / 2
        };

        SpawnRect playerRect = new SpawnRect
        {
            left = center.x - aroundPlayer.x / 2,
            right = center.x + aroundPlayer.x / 2,
            top = center.y + aroundPlayer.y / 2,
            bot = center.y - aroundPlayer.y / 2
        };
        rects.Clear();
        rects.Add(new SpawnRect
        {
            left = mapRect.left,
            right = mapRect.right,
            top = mapRect.top,
            bot = Mathf.Min(playerRect.top, mapRect.top)
        }); //플레이어 기준 위

        rects.Add(new SpawnRect
        {
            left = mapRect.left,
            right = Mathf.Max(playerRect.left, mapRect.left),
            top = Mathf.Min(playerRect.top, mapRect.top),
            bot = Mathf.Max(playerRect.bot, mapRect.bot)
        }); //플레이어 기준 왼쪽

        rects.Add(new SpawnRect
        {
            left = Mathf.Min(playerRect.right, mapRect.right),
            right = mapRect.right,
            top = Mathf.Min(playerRect.top, mapRect.top),
            bot = Mathf.Max(playerRect.bot, mapRect.bot)
        }); //플레이어 기준 오른쪽

        rects.Add(new SpawnRect
        {
            left = mapRect.left,
            right = mapRect.right,
            top = Mathf.Max(playerRect.bot, mapRect.bot),
            bot = mapRect.bot
        }); //플레이어 기준 아래

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

        await AddressableManager.Instance.RegisterAssetByKey(bossKey);
        _currentBossKey = bossKey;

        GameObject bossObject = AddressableManager.Instance.GetPrefab(bossKey);
        if (bossObject == null)
        {
            Debug.LogError($"{bossKey}: 보스 프리팹을 로드하지 못했습니다.");
            return;
        }

        Vector2 pos = SpawnablePoint();
        Instantiate(bossObject, pos, Quaternion.identity);
        Debug.Log("Boss is coming!!!");
    }
}
