using System.Collections.Generic;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;
using StatType = UpgradeDataSO.StatType;
using System;


#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

/// <summary>
/// 모든 아이템의 기반 클래스. 프리팹에 붙여 수치와 동작을 정의하고, PlayerInventory 자식으로 생성된다.
/// </summary>
public abstract class ItemBase : SerializedMonoBehaviour
{
    [Title("Info")]
    [SerializeField, PropertyOrder(0)] private string id;
    [SerializeField, PropertyOrder(0)] private Sprite icon;

    [Title("Stat")]
    [SerializeField, PropertyOrder(1)] private Dictionary<StatType, float> flatStat = new();
    [SerializeField, PropertyOrder(1)] private Dictionary<StatType, float> percentStat = new();

    [Title("Localization")]
    [SerializeField, PropertyOrder(2)] private LocalizedDataSO localizedData;

    [Title("Reference")]
    [SerializeField, PropertyOrder(2)] private UpgradeDataSO upgradeData;

    /// <summary>언어 데이터 키로 사용하는 아이템 id</summary>
    public string Id => id;
    public string NameKey => $"{id}_NAME";
    public string TextKey => $"{id}_TEXT";
    public Sprite Icon => icon;

    public IReadOnlyDictionary<StatType, float> FlatStat => flatStat;
    public IReadOnlyDictionary<StatType, float> PercentStat => percentStat;

    /// <summary>이 아이템을 보유 중인 인벤토리. 획득 전에는 null</summary>
    protected PlayerInventory Inventory { get; private set; }

    /// <summary>인벤토리에 추가된 직후 호출. 소유 인벤토리를 등록하고 재계산을 요청</summary>
    public void Acquire(PlayerInventory inventory)
    {
        Inventory = inventory;
        OnAcquire();
        RequestRecalculate();
    }

    /// <summary>인벤토리에서 제거된 직후 호출. 정리 후 재계산을 요청하고 소유 인벤토리를 해제</summary>
    public void Release()
    {
        OnRelease();
        RequestRecalculate();
        Inventory = null;
    }

    /// <summary>지정한 스탯에 값을 더하고 재계산을 요청</summary>
    public void AddStat(StatType type, bool isPercent, float value)
    {
        var dict = isPercent ? percentStat : flatStat;
        dict[type] = dict.GetValueOrDefault(type) + value;
        RequestRecalculate();
    }

    /// <summary>지정한 스탯을 지정한 값으로 덮어쓰고 재계산을 요청</summary>
    public void SetStat(StatType type, bool isPercent, float value)
    {
        var dict = isPercent ? percentStat : flatStat;
        dict[type] = value;
        RequestRecalculate();
    }

    /// <summary>이 아이템의 스탯 기여분을 합계 딕셔너리에 더한다. 조건부 스탯 등은 재정의해서 구현</summary>
    public virtual void CollectStats(Dictionary<StatType, float> flatSum, Dictionary<StatType, float> percentSum)
    {
        foreach (var (type, value) in flatStat)
            flatSum[type] = flatSum.GetValueOrDefault(type) + value;
        foreach (var (type, value) in percentStat)
            percentSum[type] = percentSum.GetValueOrDefault(type) + value;
    }

    /// <summary>획득 시 동작. 이벤트 구독 등은 재정의해서 구현</summary>
    protected virtual void OnAcquire() { }

    /// <summary>제거 시 동작. 이벤트 구독 해제 등은 재정의해서 구현</summary>
    protected virtual void OnRelease() { }

    /// <summary>보유 중인 인벤토리에 아이템 스탯 재계산을 요청</summary>
    protected void RequestRecalculate()
    {
        if (Inventory != null)
            Inventory.RecalculateItemStats();
    }

    public virtual string EffectScript()
    {
        StringBuilder sb = new StringBuilder();

        foreach (StatType e in Enum.GetValues(typeof(StatType)))
        {
            string name = localizedData.GetText($"CITEM_{e}");
            if (flatStat.TryGetValue(e, out float v))
                sb.AppendLine($"{name}:\t {(v < 0 ? "-" : "+")} {Mathf.Abs(v):0.##}");
            if (percentStat.TryGetValue(e, out v))
                sb.AppendLine($"{name}:\t {(v < 0 ? "-" : "+")} {Mathf.Abs(v * 100):0.##}%{PercentAmountText(e, v)}");
        }

        string txt = localizedData.GetText($"{id}_EFFECT");
        if (txt != "-")
            sb.AppendLine(txt);
        return sb.ToString().Trim();
    }

    /// <summary>
    /// percent 값 v가 실제로 늘리는 양을 " (+y)" 형태로 반환. y = (percent 적용 전 합계) × v.
    /// 미보유 아이템은 자신의 flat 값까지 포함해 획득 후 기준으로 계산하고, upgradeData가 없으면 빈 문자열을 반환
    /// </summary>
    private string PercentAmountText(StatType type, float v)
    {
        if (upgradeData == null)
            return string.Empty;

        float flatTotal = upgradeData.GetFlatTotal(type);
        if (Inventory == null)
            flatTotal += flatStat.GetValueOrDefault(type);

        float amount = flatTotal * v;
        return $" ({(amount < 0 ? "-" : "+")}{Mathf.Abs(amount):0.##})";
    }


#if UNITY_EDITOR
    [OnInspectorGUI, PropertyOrder(0)]
    private void DrawEffectPreview()
    {
        if (localizedData == null)
            SirenixEditorGUI.WarningMessageBox("LocalizedDataSO가 지정되지 않음");
        else if (!localizedData.IsReady)
            SirenixEditorGUI.InfoMessageBox("번역 데이터 로드 대기 중 (LocalizedDataSO의 Refresh로 갱신)");
        else
            SirenixEditorGUI.InfoMessageBox(EffectScript());
    }
#endif
}
