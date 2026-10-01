using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using StatType = UpgradeDataSO.StatType;

/// <summary>
/// 플레이어가 보유한 아이템을 획득 순서대로 관리하고, 아이템 스탯 합계를 UpgradeDataSO에 전달한다.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private UpgradeDataSO _upgradeData;

    [ShowInInspector, ReadOnly] private readonly List<ItemBase> _items = new();

    private readonly Dictionary<StatType, float> _flatSum = new();
    private readonly Dictionary<StatType, float> _percentSum = new();

    /// <summary>보유 아이템 목록 (획득 순서)</summary>
    public IReadOnlyList<ItemBase> Items => _items;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        //시작시 SO 초기화
        RecalculateItemStats();
    }

    /// <summary>아이템 프리팹을 자식으로 생성해 목록 끝에 추가하고, 생성된 인스턴스를 반환</summary>
    [Button]
    public ItemBase AddItem(ItemBase prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[PlayerInventory] 추가할 아이템 프리팹이 null");
            return null;
        }

        var item = Instantiate(prefab, transform);
        _items.Add(item);
        item.Acquire(this);
        OnInventoryChanged?.Invoke();
        return item;
    }

    /// <summary>보유 중인 아이템 인스턴스를 목록에서 제거하고 파괴</summary>
    public void RemoveItem(ItemBase item)
    {
        if (!_items.Remove(item))
        {
            Debug.LogWarning($"[PlayerInventory] 보유하지 않은 아이템: {(item != null ? item.name : "null")}");
            return;
        }

        item.Release();
        Destroy(item.gameObject);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>목록의 index 번째 아이템을 제거</summary>
    [Button]
    public void RemoveItemAt(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            Debug.LogWarning($"[PlayerInventory] 범위를 벗어난 index: {index}");
            return;
        }

        RemoveItem(_items[index]);
    }

    public void RecalculateItemStats()
    {
        _flatSum.Clear();
        _percentSum.Clear();
        foreach (var item in _items)
            item.CollectStats(_flatSum, _percentSum);

        _upgradeData.SetItemStats(_flatSum, _percentSum);
    }
}
