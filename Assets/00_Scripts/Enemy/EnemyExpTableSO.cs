using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyExpTableSO", menuName = "Scriptable Objects/EnemyExpTableSO")]
public class EnemyExpTableSO : ScriptableObject
{
    [SerializeField] int defaultEXP = 5;
    [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
    [LabelText("Enemy EXP Table")]
    [SerializeField] private List<EnemyExpEntry> expTable = new();

    private Dictionary<Type, int> _cache;

    private void OnEnable()
    {
        RebuildCache();
    }

    private void OnValidate()
    {
        RebuildCache();
    }

    private void RebuildCache()
    {
        _cache = new Dictionary<Type, int>();
        foreach (var entry in expTable)
        {
            var type = entry.targetType;
            if (type is null)
                continue;

            if (_cache.ContainsKey(type))
            {
                Debug.LogWarning($"{name}: '{type.Name}' 중복 등록");
                continue;
            }

            _cache[type] = entry.exp;
        }
    }

    public int GetEXP(Type type)
    {
        if (_cache is null)
            RebuildCache();

        return _cache.TryGetValue(type, out int exp) ? exp : defaultEXP;
    }
}

[Serializable]
public class EnemyExpEntry
{
    [ValueDropdown("GetFilteredTypes")]
    [LabelText("Enemy")]
    public string TypeName;

    [LabelText("EXP")]
    public int exp;

    public Type targetType
    {
        get
        {
            if (string.IsNullOrEmpty(TypeName)) return null;
            return Type.GetType(TypeName);
        }
    }

    private static IEnumerable<ValueDropdownItem> GetFilteredTypes()
    {
        var items = new List<ValueDropdownItem>();
#if UNITY_EDITOR
        var types = UnityEditor.TypeCache.GetTypesDerivedFrom<EnemyBase>().Where(t => !t.IsAbstract && !t.IsGenericType);
        foreach (var t in types)
        {
            items.Add(new ValueDropdownItem(t.Name, t.AssemblyQualifiedName));
        }
#endif
        return items;
    }
}