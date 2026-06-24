using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemySpawner : MonoBehaviour
{
    private Dictionary<System.Type, IObjectPool<GameObject>> _poolDict = new();
    private Dictionary<System.Type, GameObject> _poolPrepab = new();

    /// <summary>
    /// 풀링 대상 타입에 사용할 프리팹을 등록합니다.
    /// </summary>
    public void RegisterPrefab(System.Type type, GameObject prefab)
    {
        _poolPrepab[type] = prefab;
    }

    public GameObject Spawn(System.Type type)
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
        GameObject obj = Spawn(type);
        obj.transform.position = position;

        EnemyBase enemy = obj.GetComponent<EnemyBase>();
        if (enemy != null)
            enemy.SetSpawner(this);

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
}
