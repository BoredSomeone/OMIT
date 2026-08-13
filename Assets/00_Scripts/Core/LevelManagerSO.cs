using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "LevelManagerSO", menuName = "Scriptable Objects/LevelManager")]
public class LevelManagerSO : ScriptableObject
{
    [Serializable] public struct EnemyWeightEntry
    {
        [ValueDropdown("GetFilteredTypes")]
        [LabelText("Enemy")]
        public string TypeName;

        [LabelText("Weight")]
        public int weight;

        public Type targetType => string.IsNullOrEmpty(TypeName) ? null : Type.GetType(TypeName);

        private static IEnumerable<ValueDropdownItem> GetFilteredTypes()
        {
            var items = new List<ValueDropdownItem>();
#if UNITY_EDITOR
            var types = UnityEditor.TypeCache.GetTypesDerivedFrom<EnemyBase>().Where(t => !t.IsAbstract && !t.IsGenericType);
            foreach (var t in types)
                items.Add(new ValueDropdownItem(t.Name, t.AssemblyQualifiedName));
#endif
            return items;
        }
    }
    [Serializable] public struct SpawnInfo
    {
        public int level;

        [ValueDropdown("GetFilteredBossTypes")]
        public string BossKey;

        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        public List<EnemyWeightEntry> EnemyWeights;

        private static IEnumerable<ValueDropdownItem> GetFilteredBossTypes()
        {
            var items = new List<ValueDropdownItem>();
#if UNITY_EDITOR
            var types = UnityEditor.TypeCache.GetTypesDerivedFrom<EnemyBase>().Where(t => !t.IsAbstract && !t.IsGenericType);
            foreach (var t in types)
                items.Add(new ValueDropdownItem(t.Name, t.Name));
#endif
            return items;
        }

        public Type SpawnTarget
        {
            get
            {
                Type chosen = null;
                float runningTotal = 0;
                foreach (var ew in EnemyWeights)
                {
                    var type = ew.targetType;
                    if (type is null) continue;
                    runningTotal += ew.weight;
                    if (UnityEngine.Random.Range(0, ew.weight) < ew.weight)
                        chosen = type;
                }
                return chosen;
            }
        }
    }

    [SerializeField] private int _baseRequireEXP = 25;

    [SerializeField] private int _baseNumSpawnEnemy = 1;
    [SerializeField] private float _baseSpawnEnemyDelay = 3;

    [SerializeField, ReadOnly] private int _level = 1;
    [SerializeField, ReadOnly] private int _nowEXP;

    [SerializeField, ReadOnly] private int _requireEXP;
    [SerializeField, ReadOnly] private int _baseHP;
    [SerializeField, ReadOnly] private int _numEnemySpawn;
    [SerializeField, ReadOnly] private float _enemySpawnDelay;
    [ListDrawerSettings(ListElementLabelName = "level")]
    [SerializeField] private SpawnInfo[] _spawnInfos;
    [SerializeField, ReadOnly] private SpawnInfo _spawnInfo;

    public int baseHP { get { return _baseHP; } }
    public int level { get { return _level; } }
    public int numEnemySpawn { get { return _numEnemySpawn; } }
    public float enemySpawnDelay { get { return _enemySpawnDelay; } }
    public int requireEXP { get { return _requireEXP; } }
    public int nowEXP { get { return _nowEXP; } }

    public SpawnInfo spawnInfo { get { return _spawnInfo; } }


    public UnityEvent<string> spawnBossEvent = new();
    public UnityEvent spawnInfoUpdateEvent = new();
    public UnityEvent expUpEvent = new();
    public UnityEvent LevelUpEvent = new();

    private void OnEnable()
    {
        ReCalcStats(false);
        _spawnInfos = _spawnInfos.OrderBy(x => x.level).ToArray();
    }

    public void AddEXP(int exp)
    {
        _nowEXP += exp;
        if (requireEXP > 0)
        {
            while (nowEXP >= requireEXP)
            {
                _nowEXP -= requireEXP;
                LevelUp();
            }
        }
        expUpEvent.Invoke();
    }

    private int LevelUp()
    {
        ++_level;
        Debug.Log($"LEVEL UP! -> {level}");

        ReCalcStats();
        expUpEvent?.Invoke();
        LevelUpEvent?.Invoke();
        return level;
    }

    public void LevelSet(int level)
    {
        _level = level;
        ReCalcStats(false);
    }

    public void ExpSet(int exp)
    {
        _nowEXP = exp;

        expUpEvent.Invoke();
    }

    public void ReCalcStats(bool triggerSpawnEvent = true)
    {
        _requireEXP = Mathf.FloorToInt(_baseRequireEXP * Mathf.Pow(1.015f, level));
        _baseHP = Mathf.FloorToInt(100 * (1 + level * 0.05f) * (1 + (level / 5) * 0.3f));

        _numEnemySpawn = _baseNumSpawnEnemy + Mathf.FloorToInt(level / 10);
        _enemySpawnDelay = Mathf.Max(0.3f, _baseSpawnEnemyDelay - level * 0.3f);

        UpdateSpawnInfo(triggerSpawnEvent);
        expUpEvent?.Invoke();
    }

    private void UpdateSpawnInfo(bool triggerSpawnEvent)
    {
        for (int i = 0; i < _spawnInfos.Length; ++i)
        {
            SpawnInfo info = _spawnInfos[i];
            if (info.level == level)
            {
                _spawnInfo = info;
                if (triggerSpawnEvent)
                    spawnBossEvent?.Invoke(info.BossKey);

                spawnInfoUpdateEvent?.Invoke();
                break;
            }
        }
    }
}
