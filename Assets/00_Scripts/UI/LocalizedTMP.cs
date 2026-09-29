using Sirenix.OdinInspector;
using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedTMP : MonoBehaviour
{
    [Required("LocalizedDataSO가 지정되지 않았습니다.")]
    [SerializeField] private LocalizedDataSO localizedData;

    [ValueDropdown(nameof(GetIdDropdown), AppendNextDrawer = true, DisableGUIInAppendedDrawer = false)]
    [InfoBox("$" + nameof(IdWarning), InfoMessageType.Warning, nameof(HasIdWarning))]
    [SerializeField] private string id;

    [InfoBox("$" + nameof(FontWarning), InfoMessageType.Warning, nameof(HasFontWarning))]
    [SerializeField] private FontType fontType = FontType.Body;

    [SerializeField] private bool keepFont;

    private TextMeshProUGUI _tmp;
    private TextMeshProUGUI tmp
    {
        get
        {
            if (_tmp is null)
                _tmp = GetComponent<TextMeshProUGUI>();
            return _tmp;
        }
    }
    private LocalizedDataSO subscribed;

    private void OnEnable()
    {
        Subscribe();
        Apply();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void SetId(string newId)
    {
        id = newId;
        Apply();
    }

    public void Apply()
    {
        if (localizedData == null || !localizedData.IsReady)
            return;

        if (!keepFont)
        {
            var font = localizedData.GetFont(fontType);
            if (font != null && tmp.font != font)
                tmp.font = font;
        }

        if (!string.IsNullOrEmpty(id))
            tmp.text = localizedData.GetText(id);
    }

    private void Subscribe()
    {
        Unsubscribe();
        if (localizedData == null)
            return;

        localizedData.OnLanguageChangedEvent += Apply;
        subscribed = localizedData;
    }


    private void Unsubscribe()
    {
        if (subscribed != null)
            subscribed.OnLanguageChangedEvent -= Apply;
        subscribed = null;
    }

    #region inspector
    private bool HasIdWarning => !string.IsNullOrEmpty(IdWarning);
    private bool HasFontWarning => !string.IsNullOrEmpty(FontWarning);
    private ValueDropdownList<string> GetIdDropdown()
    {
#if UNITY_EDITOR
        if (localizedData != null)
            return localizedData.GetIdDropdown();
#endif
        return new ValueDropdownList<string>();
    }

    /// <summary>
    /// id가 비었거나 현재 미리보기 언어의 JSON에 없으면 경고 문구를, 아니면 null을 반환합니다.
    /// </summary>
    private string IdWarning
    {
        get
        {
            if (string.IsNullOrEmpty(id))
                return "id가 비어 있습니다.";
            if (localizedData != null && localizedData.IsReady && !localizedData.HasText(id))
                return $"JSON에 없는 id입니다. ({localizedData.CurrentLanguage} 기준)";
            return null;
        }
    }

    /// <summary>
    /// 글꼴을 교체하는 설정인데 현재 언어의 fontType 글꼴이 비어 있으면 경고 문구를, 아니면 null을 반환합니다.
    /// </summary>
    private string FontWarning
    {
        get
        {
            if (keepFont || localizedData == null || !localizedData.IsReady)
                return null;
            if (localizedData.GetFont(fontType) == null)
                return $"{localizedData.CurrentLanguage}의 {fontType} 글꼴이 지정되지 않았습니다.";
            return null;
        }
    }
    #endregion

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;
        EditorApplication.delayCall += () =>
        {
            if (this == null || !isActiveAndEnabled)
                return;

            Subscribe();
            Apply();
        };
    }
#endif
}
