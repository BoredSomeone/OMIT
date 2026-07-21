using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableManager
{
    static AddressableManager _instance;


    private readonly Dictionary<string, AsyncOperationHandle> _handles = new();
    private readonly Dictionary<string, GameObject> _prefabs = new();

    public static AddressableManager Instance
    {
        get
        {
            if (_instance is null)
                _instance = new();
            return _instance;
        }
    }

    public IReadOnlyDictionary<string, GameObject> Prefabs => _prefabs;

    /// <summary>
    /// label에 해당하는 GameObject 어드레서블을 전부 로드해 등록합니다.
    /// </summary>
    public async UniTask RegistAsset(string label)
    {
        var locHandle = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
        var locations = await locHandle;

        foreach (var loc in locations)
        {
            if (!_prefabs.ContainsKey(loc.PrimaryKey))
            {
                var assetHandle = Addressables.LoadAssetAsync<GameObject>(loc);
                _handles[loc.PrimaryKey] = assetHandle;
                _prefabs[loc.PrimaryKey] = await assetHandle;
            }
        }

        Addressables.Release(locHandle);
    }

    /// <summary>
    /// key에 해당하는 GameObject 어드레서블 하나만 로드해 등록합니다. 이미 로드되어 있으면 건너뜁니다.
    /// </summary>
    public async UniTask RegistAssetByKey(string key)
    {
        if (_prefabs.ContainsKey(key))
            return;

        var assetHandle = Addressables.LoadAssetAsync<GameObject>(key);
        _handles[key] = assetHandle;
        _prefabs[key] = await assetHandle;
    }

    /// <summary>
    /// 등록된 프리팹을 주소(키) 기준으로 가져옵니다.
    /// </summary>
    public GameObject GetPrefab(string key)
    {
        _prefabs.TryGetValue(key, out var prefab);
        return prefab;
    }

    /// <summary>
    /// key에 해당하는 로드된 에셋 하나만 해제합니다.
    /// </summary>
    public void ReleaseKey(string key)
    {
        if (_handles.TryGetValue(key, out var handle))
        {
            if (handle.IsValid())
                Addressables.Release(handle);
            _handles.Remove(key);
        }
        _prefabs.Remove(key);
    }

    /// <summary>
    /// 로드한 Addressables 핸들을 모두 해제합니다.
    /// </summary>
    public void ReleaseAll()
    {
        foreach (var handle in _handles.Values)
            if (handle.IsValid())
                Addressables.Release(handle);
        _handles.Clear();
        _prefabs.Clear();
    }
}
