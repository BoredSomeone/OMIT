using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

public struct AddressableLabels
{
    public const string NormalEnemy = "NormalEnemy";
    public const string ItemData = "ItemData";
    public const string LocalizedText = "LocalizedText";
}

public class AddressableManager
{
    static AddressableManager _instance;


    private readonly Dictionary<string, AsyncOperationHandle> _handles = new();
    private readonly Dictionary<string, Object> _assets = new();
    private readonly Dictionary<string, GameObject> _prefabs = new();
    private readonly Dictionary<string, UniTask<Object>> _loading = new();

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
    public async UniTask RegisterAsset(string label)
    {
        await LoadByLabel<GameObject>(label);
    }

    /// <summary>
    /// key에 해당하는 GameObject 어드레서블 하나만 로드해 등록합니다. 이미 로드되어 있으면 건너뜁니다.
    /// </summary>
    public async UniTask RegisterAssetByKey(string key)
    {
        await LoadByKey<GameObject>(key);
    }

    /// <summary>
    /// 등록된 프리팹을 주소(키) 기준으로 가져옵니다.
    /// </summary>
    public GameObject GetPrefab(string key)
    {
        return Get<GameObject>(key);
    }

    /// <summary>
    /// label에 해당하는 T 타입 에셋을 전부 로드해 캐싱하고, 로드된 목록을 반환합니다.
    /// </summary>
    public async UniTask<IReadOnlyList<T>> LoadByLabel<T>(string label) where T : Object
    {
        var locHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
        try
        {
            var locations = await locHandle;
            var assets = await UniTask.WhenAll(locations.Select(loc => LoadCached<T>(loc.PrimaryKey, loc)));
            return assets.Where(asset => asset != null).ToList();
        }
        finally
        {
            Addressables.Release(locHandle);
        }
    }

    /// <summary>
    /// key에 해당하는 T 타입 에셋 하나를 로드해 캐싱하고 반환합니다. 이미 로드되어 있으면 캐시를 반환합니다.
    /// </summary>
    public async UniTask<T> LoadByKey<T>(string key) where T : Object
    {
        return await LoadCached<T>(key, key);
    }

    /// <summary>
    /// 캐싱된 에셋을 키 기준으로 가져옵니다. 없거나 타입이 다르면 null을 반환합니다.
    /// </summary>
    public T Get<T>(string key) where T : Object
    {
        _assets.TryGetValue(key, out var asset);
        return asset as T;
    }

    /// <summary>
    /// key 또는 label에 해당하는 T 타입 에셋을 로드해 onLoaded를 호출한 뒤 즉시 해제합니다. (캐싱하지 않음)
    /// JSON 같이 한 번 읽고 버리는 데이터에 사용합니다.
    /// </summary>
    public async UniTask LoadOnce<T>(string keyOrLabel, Action<T> onLoaded) where T : Object
    {
        var locHandle = Addressables.LoadResourceLocationsAsync(keyOrLabel, typeof(T));
        try
        {
            var locations = await locHandle;
            foreach (var loc in locations)
            {
                var assetHandle = Addressables.LoadAssetAsync<T>(loc);
                try
                {
                    onLoaded?.Invoke(await assetHandle);
                }
                finally
                {
                    Addressables.Release(assetHandle);
                }
            }
        }
        finally
        {
            Addressables.Release(locHandle);
        }
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
        _assets.Remove(key);
        _prefabs.Remove(key);
        _loading.Remove(key);
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
        _assets.Clear();
        _prefabs.Clear();
        _loading.Clear();
    }

    /// <summary>
    /// 캐시 → 진행 중인 로드 → 신규 로드 순으로 확인해 에셋을 반환합니다. 같은 키의 동시 요청은 하나의 로드를 공유합니다.
    /// </summary>
    private async UniTask<T> LoadCached<T>(string cacheKey, object loadKey) where T : Object
    {
        if (_assets.TryGetValue(cacheKey, out var cached))
            return cached as T;

        if (!_loading.TryGetValue(cacheKey, out var task))
        {
            task = LoadInternal<T>(cacheKey, loadKey).Preserve();
            _loading[cacheKey] = task;
        }

        var asset = await task;
        _loading.Remove(cacheKey);
        return asset as T;
    }

    /// <summary>
    /// 실제 Addressables 로드를 수행하고 성공 시 캐시에 등록합니다. 실패 시 핸들을 해제하고 null을 반환합니다.
    /// </summary>
    private async UniTask<Object> LoadInternal<T>(string cacheKey, object loadKey) where T : Object
    {
        // object로 넘기면 IResourceLocation 오버로드가 선택되지 않으므로 명시적으로 분기
        var handle = loadKey is IResourceLocation location
            ? Addressables.LoadAssetAsync<T>(location)
            : Addressables.LoadAssetAsync<T>(loadKey);
        _handles[cacheKey] = handle;

        try
        {
            await handle;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[AddressableManager] 로드 실패: {cacheKey} ({typeof(T).Name})");
            _handles.Remove(cacheKey);
            Addressables.Release(handle);
            return null;
        }

        _assets[cacheKey] = handle.Result;
        if (handle.Result is GameObject prefab)
            _prefabs[cacheKey] = prefab;

        return handle.Result;
    }
}
