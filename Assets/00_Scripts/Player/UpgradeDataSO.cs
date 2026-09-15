using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "UpgradeDataSO", menuName = "Scriptable Objects/UpgradeDataSO")]
public class UpgradeDataSO : SerializedScriptableObject
{

    public struct Item
    {
        public Dictionary<StatType, float> flatStat;
        public Dictionary<StatType, float> percentStat;
    }
    public enum StatType
    {
        AttackDamage,
        AttackSpeed,
        SightAngle,
        SightRadius,
        BulletSpeed,
        MoveSpeed,
        WeaponMoveSpeed,
        WeaponMoveAccel,
        WeaponRotateSpeed,
        WeaponRotateAccel,
    }

    [SerializeField] PlayerDefaultStatDataSO _playerDefaultStatData;

    [Title("Upgrade")]
    [ReadOnly] private Dictionary<StatType, float> flat = new();
    [ReadOnly] private Dictionary<StatType, float> percent = new();
    [ReadOnly] private Dictionary<StatType, float> cached = new();

    static readonly Dictionary<StatType, Func<PlayerDefaultStatDataSO, float>> baseGetters = new()
    {
        { StatType.AttackDamage,      d => d.baseWeaponAttackDamage },
        { StatType.AttackSpeed,       d => d.baseWeaponAttackSpeed },
        { StatType.SightAngle,        d => d.baseAttackSightAngle },
        { StatType.SightRadius,       d => d.baseAttackSightRadius },
        { StatType.BulletSpeed,       d => d.baseBulletSpeed },
        { StatType.MoveSpeed,         d => d.baseMaxSpeed },
        { StatType.WeaponMoveSpeed,   d => d.baseWeaponMoveSpeed },
        { StatType.WeaponMoveAccel,   d => d.baseWeaponMoveAccel },
        { StatType.WeaponRotateSpeed, d => d.baseWeaponRotateSpeed },
        { StatType.WeaponRotateAccel, d => d.baseWeaponRotateAccel },
    };

    [ReadOnly] private int nextInstanceId = 0;
    private SortedDictionary<int, Item> hasItemDict = new();
    private Dictionary<string, Item> ItemDict = new();

    public event Action OnWeaponStatChanged;


    private async void OnEnable()
    {
        OnWeaponStatChanged += CalculateStats;
        await LoadItemInfos();
        CalculateStats();
    }

    private void OnDisable()
    {
        OnWeaponStatChanged -= CalculateStats;
    }

    /// <summary>id에 해당하는 아이템 정의를 복제해 보유 목록에 추가하고 능력치를 재계산</summary>
    [Button]
    public void AddItem(string id)
    {
        if (!ItemDict.TryGetValue(id, out var def))
        {
            Debug.LogWarning($"ItemDict에 존재하지 않는 아이템 id: {id}");
            return;
        }

        var item = new Item
        {
            flatStat = new Dictionary<StatType, float>(def.flatStat),
            percentStat = new Dictionary<StatType, float>(def.percentStat),
        };
        hasItemDict[nextInstanceId++] = item;
        OnWeaponStatChanged?.Invoke();
    }

    /// <summary>보유 중인 특정 인스턴스의 스탯 값을 더하고 능력치를 재계산</summary>
    public void EditItem(int instanceId, StatType type, bool isPercent, float value)
    {
        if (!hasItemDict.TryGetValue(instanceId, out var item))
        {
            Debug.LogWarning($"hasItemDict에 존재하지 않는 instanceId: {instanceId}");
            return;
        }

        var dict = isPercent ? item.percentStat : item.flatStat;
        dict[type] = dict.GetValueOrDefault(type) + value;
        OnWeaponStatChanged?.Invoke();
    }

    /// <summary>보유 중인 특정 인스턴스를 제거하고 능력치를 재계산한다.</summary>
    public void RemoveItem(int instanceId)
    {
        hasItemDict.Remove(instanceId);
        OnWeaponStatChanged?.Invoke();
    }

    /// <summary>지정한 타입에 업그레이드 값을 더하고 능력치 재계산</summary>
    [Button]
    public void AddUpgrade(StatType type, bool isPercent, float value)
    {
        var dict = isPercent ? percent : flat;
        dict[type] = dict.GetValueOrDefault(type) + value;
        OnWeaponStatChanged?.Invoke();
    }

    /// <summary>지정한 타입의 업그레이드 값을 지정한 값으로 덮어쓰고 재계산</summary>
    public void ToUpgradeValue(StatType type, bool isPercent, float value)
    {
        var dict = isPercent ? percent : flat;
        dict[type] = value;
        OnWeaponStatChanged?.Invoke();
    }

    /// <summary>모든 flat/percent 업그레이드 값을 초기화하고 재계산</summary>
    public void ResetAllUpgrade()
    {
        flat.Clear();
        percent.Clear();
        OnWeaponStatChanged?.Invoke();
    }

    /// <summary>업그레이드가 반영된 최종 스탯 값을 반환</summary>
    public float GetCached(StatType type) => cached.GetValueOrDefault(type);

    private void CalculateStats()
    {
        foreach (var (type, getBase) in baseGetters)
        {
            float baseValue = getBase(_playerDefaultStatData);

            float itemFlatSum = 0f;
            float itemPercentSum = 0f;
            foreach (var item in hasItemDict.Values)
            {
                itemFlatSum += item.flatStat.GetValueOrDefault(type);
                itemPercentSum += item.percentStat.GetValueOrDefault(type);
            }

            cached[type] = (baseValue + flat.GetValueOrDefault(type) + itemFlatSum)
                           * (1 + percent.GetValueOrDefault(type) + itemPercentSum);
        }
    }

    private async UniTask LoadItemInfos()
    {
        var locHandle = Addressables.LoadResourceLocationsAsync(AddressableLabels.ItemData, typeof(TextAsset));
        var locations = await locHandle;

        foreach (var loc in locations)
        {
            var assetHandle = Addressables.LoadAssetAsync<TextAsset>(loc);
            var textAsset = await assetHandle;
            ParseItemData(textAsset.text);
            Addressables.Release(assetHandle);
        }

        Addressables.Release(locHandle);
    }

    /// <summary>ItemDatas.json 원본 배열을 id 기준으로 묶어 ItemDict를 채움</summary>
    private void ParseItemData(string json)
    {
        var rows = JArray.Parse(json);
        foreach (var group in rows.GroupBy(row => (string)row["id"]))
        {
            var item = new Item
            {
                flatStat = new Dictionary<StatType, float>(),
                percentStat = new Dictionary<StatType, float>(),
            };

            foreach (var row in group)
            {
                bool isPercent = (string)row["statType"] == "percent";
                var dict = isPercent ? item.percentStat : item.flatStat;

                foreach (StatType type in Enum.GetValues(typeof(StatType)))
                {
                    var token = row[type.ToString()];
                    if (token != null)
                        dict[type] = token.Value<float>();
                }
            }

            ItemDict[group.Key] = item;
        }
    }
}
