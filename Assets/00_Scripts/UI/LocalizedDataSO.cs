using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public enum FontType
{
    Header,
    Body,
}



[CreateAssetMenu(fileName = "LocalizedDataSO", menuName = "Scriptable Objects/LocalizedDataSO")]
public class LocalizedDataSO : ScriptableObject
{
    [Serializable]
    public class LanguageFontSet
    {
        [Serializable]
        public class FontSlot
        {
            public FontType type;
            public AssetReferenceT<TMP_FontAsset> font;
        }

        public SystemLanguage language;
        public List<FontSlot> slots = new();

        public AssetReferenceT<TMP_FontAsset> Get(FontType type)
        {
            var slot = slots.Find(s => s.type == type);
            return slot?.font != null && slot.font.RuntimeKeyIsValid() ? slot.font : null;
        }
    }

    const int ExternalFontSamplingSize = 90;
    const int ExternalFontPadding = 9;
    const int ExternalFontAtlasSize = 1024;

    #region field
    [Title("글꼴")]
    [SerializeField] List<LanguageFontSet> fontSets = new();

    [Title("대체 언어")]
    [InfoBox("번역 또는 글꼴이 없는 언어는 이 언어의 값을 사용합니다.")]
    [SerializeField] SystemLanguage fallbackLanguage = SystemLanguage.Korean;

#if UNITY_EDITOR
    [Title("에디터 미리보기")]
    [SerializeField] SystemLanguage previewLanguage = SystemLanguage.Korean;

    // id 드롭다운용 (파일명, id, 미리보기 텍스트)
    List<(string file, string id, string text)> editorIdEntries = new();
#endif

    [Title("런타임")]
    [ShowInInspector, ReadOnly] SystemLanguage currentLanguage;
    [ShowInInspector, ReadOnly] Dictionary<FontType, TMP_FontAsset> fonts = new();
    [ShowInInspector, ReadOnly] Dictionary<string, string> texts = new();

    // 현재 적용 중인 Addressable 글꼴 키 / 외부 폴더에서 생성한 글꼴
    readonly List<string> loadedFontKeys = new();
    readonly List<TMP_FontAsset> externalFonts = new();

    bool isChanging;
    SystemLanguage requestedLanguage;

    /// <summary>글꼴과 텍스트가 교체된 직후 호출됩니다.</summary>
    public event Action OnLanguageChangedEvent;

    public bool IsReady { get; private set; }
    public SystemLanguage CurrentLanguage => currentLanguage;
    #endregion

    private void OnEnable()
    {
        ResetRuntimeState();
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            QueueEditorPreview();
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
    }

    #region public
    /// <summary>
    /// 현재 언어의 type 글꼴을 반환합니다. 로드되지 않았으면 null을 반환합니다.
    /// </summary>
    public TMP_FontAsset GetFont(FontType type)
    {
        fonts.TryGetValue(type, out var font);
        return font;
    }

    /// <summary>
    /// 현재 언어의 id 텍스트를 반환합니다. 없으면 id를 그대로 반환합니다.
    /// </summary>
    public string GetText(string id)
    {
        string key = NormalizeId(id);
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        return texts.TryGetValue(key, out var text) ? text : id;
    }

    /// <summary>
    /// 현재 언어(대체 언어 포함)에 id 텍스트가 있는지 확인합니다.
    /// </summary>
    public bool HasText(string id)
    {
        string key = NormalizeId(id);
        return !string.IsNullOrEmpty(key) && texts.ContainsKey(key);
    }

    /// <summary>
    /// id 비교용 키를 반환합니다. 앞뒤 공백을 지우고 대문자로 변환합니다. (hello, Hello, HELLO → HELLO)
    /// </summary>
    public static string NormalizeId(string id)
    {
        return id?.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// 언어를 변경합니다. 새 글꼴과 텍스트를 모두 로드한 뒤 교체하고 OnLanguageChanged를 호출합니다.
    ///
    /// 로드 중 다시 호출되면 진행 중인 로드가 끝난 뒤 마지막으로 요청된 언어만 적용합니다.
    /// </summary>
    public async UniTask ChangeLanguage(SystemLanguage language)
    {
        requestedLanguage = language;
        if (isChanging)
            return;

        if (IsReady && currentLanguage == language)
            return;

        isChanging = true;
        try
        {
            SystemLanguage target;
            do
            {
                target = requestedLanguage;
                await ApplyLanguage(target);
            }
            while (requestedLanguage != target);
        }
        finally
        {
            isChanging = false;
        }
    }
    #endregion

    #region load
    /// <summary>
    /// language의 텍스트와 글꼴을 로드해 교체하고, 이벤트 호출 후 이전 글꼴을 해제합니다.
    /// </summary>
    private async UniTask ApplyLanguage(SystemLanguage language)
    {
        var newTexts = new Dictionary<string, string>();
        await AddressableManager.Instance.LoadOnce<TextAsset>(AddressableLabels.LocalizedText, textAsset => ParseTexts(textAsset.text, language, newTexts));

        var newFonts = new Dictionary<FontType, TMP_FontAsset>();
        var newKeys = new List<string>();
        foreach (FontType type in Enum.GetValues(typeof(FontType)))
        {
            var reference = GetFontReference(language, type);
            if (reference == null)
            {
                Debug.LogWarning($"[LocalizedDataSO] {language}의 {type} 글꼴이 지정되지 않았습니다.");
                continue;
            }

            var font = await AddressableManager.Instance.LoadByKey<TMP_FontAsset>(reference.AssetGUID);
            if (font == null)
                continue;

            newFonts[type] = font;
            newKeys.Add(reference.AssetGUID);
        }

        var newExternalFonts = LoadExternalFonts(language, newFonts);

        var oldKeys = loadedFontKeys.Except(newKeys).ToList();
        var oldExternalFonts = externalFonts.ToList();

        texts = newTexts;
        fonts = newFonts;
        loadedFontKeys.Clear();
        loadedFontKeys.AddRange(newKeys);
        externalFonts.Clear();
        externalFonts.AddRange(newExternalFonts);
        currentLanguage = language;
        IsReady = true;

        OnLanguageChangedEvent?.Invoke();

        // 모든 텍스트가 새 글꼴로 교체된 뒤 해제
        foreach (var key in oldKeys)
            AddressableManager.Instance.ReleaseKey(key);
        foreach (var font in oldExternalFonts)
            DestroyFontAsset(font, false);
    }

    /// <summary>
    /// 로컬라이징 JSON 배열에서 language 열을 읽어 result에 채웁니다. 번역이 없으면 대체 언어 값을 사용합니다.
    ///
    /// result의 키는 NormalizeId로 변환한 id입니다. rawEntries를 넘기면 원래 표기(앞뒤 공백 제거)의 id와 텍스트를 함께 모읍니다.
    /// </summary>
    private void ParseTexts(string json, SystemLanguage language, Dictionary<string, string> result, List<(string id, string text)> rawEntries = null)
    {
        JArray rows;
        try
        {
            rows = JArray.Parse(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalizedDataSO] JSON 파싱 실패\n{e.Message}");
            return;
        }

        string languageKey = language.ToString();
        string fallbackKey = fallbackLanguage.ToString();

        foreach (var row in rows)
        {
            string id = ((string)row["id"])?.Trim();
            if (string.IsNullOrEmpty(id))
                continue;
            string key = NormalizeId(id);

            string text;
            if (!string.IsNullOrEmpty((string)row[languageKey]))
                text = (string)row[languageKey];
            else
            {
                text = ((string)row[fallbackKey] + " (" + id + ")");
                Debug.LogWarning($"{languageKey} / {id}가 비어있어요");
            }
            if (text == null)
                continue;

            if (result.ContainsKey(key))
                Debug.LogWarning($"[LocalizedDataSO] 중복된 id: {id} (대소문자 구분 없음: {key})");

            result[key] = text;
            rawEntries?.Add((id, text));
        }
    }

    /// <summary>
    /// language의 type 글꼴 레퍼런스를 반환합니다. 없으면 대체 언어의 레퍼런스를 반환합니다.
    /// </summary>
    private AssetReferenceT<TMP_FontAsset> GetFontReference(SystemLanguage language, FontType type)
    {
        return fontSets.Find(s => s.language == language)?.Get(type)
               ?? fontSets.Find(s => s.language == fallbackLanguage)?.Get(type);
    }
    #endregion

    #region external font
    /// <summary>
    /// Fonts.ini에 지정된 외부 글꼴 파일로 글꼴 에셋을 생성해 fontTable의 해당 슬롯을 덮어씁니다.
    /// 기존 글꼴은 외부 글꼴의 Fallback으로 등록되며, ini가 없으면 빈 템플릿을 생성합니다.
    /// </summary>
    private List<TMP_FontAsset> LoadExternalFonts(SystemLanguage language, Dictionary<FontType, TMP_FontAsset> fontTable)
    {
        var created = new List<TMP_FontAsset>();
        var ini = LoadExternalFontIni();
        if (ini == null || !ini.TryGetSection(language.ToString(), out var section))
            return created;

        foreach (FontType type in Enum.GetValues(typeof(FontType)))
        {
            string fileName = section[type.ToString()].GetString();
            if (string.IsNullOrEmpty(fileName))
                continue;

            fontTable.TryGetValue(type, out var baseFont);
            var font = CreateExternalFont(fileName, baseFont);
            if (font == null)
                continue;

            fontTable[type] = font;
            created.Add(font);
        }

        return created;
    }

    /// <summary>
    /// Fonts.ini를 읽어 반환합니다. 없으면 모든 언어/글꼴의 빈 항목으로 템플릿을 생성하고 null을 반환합니다.
    /// </summary>
    private IniFile LoadExternalFontIni()
    {
        var ini = new IniFile();
        if (File.Exists(GlobalValue.ExternalFontIniPath))
        {
            try
            {
                ini.Load(GlobalValue.ExternalFontIniPath);
                return ini;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LocalizedDataSO] 외부 글꼴 설정을 읽지 못했습니다.\t{GlobalValue.ExternalFontIniPath}\n{e.Message}");
                return null;
            }
        }

        try
        {
            var languages = fontSets.Select(s => s.language).Append(fallbackLanguage).Distinct();
            foreach (var language in languages)
                foreach (FontType type in Enum.GetValues(typeof(FontType)))
                    ini[language.ToString()][type.ToString()] = string.Empty;

            Directory.CreateDirectory(GlobalValue.ExternalFontDirectory);
            ini.Save(GlobalValue.ExternalFontIniPath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LocalizedDataSO] 외부 글꼴 설정 템플릿을 만들지 못했습니다.\t{GlobalValue.ExternalFontIniPath}\n{e.Message}");
        }

        return null;
    }

    /// <summary>
    /// 외부 폴더의 글꼴 파일로 Dynamic 글꼴 에셋을 생성합니다. 실패하면 null을 반환합니다.
    /// </summary>
    private TMP_FontAsset CreateExternalFont(string fileName, TMP_FontAsset fallback)
    {
        string path = Path.Combine(GlobalValue.ExternalFontDirectory, fileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[LocalizedDataSO] 외부 글꼴 파일이 없습니다.\t{path}");
            return null;
        }

        var font = TMP_FontAsset.CreateFontAsset(path, 0, ExternalFontSamplingSize, ExternalFontPadding, GlyphRenderMode.SDFAA, ExternalFontAtlasSize, ExternalFontAtlasSize);
        if (font == null)
        {
            Debug.LogWarning($"[LocalizedDataSO] 외부 글꼴을 불러오지 못했습니다.\t{path}");
            return null;
        }

        font.name = Path.GetFileNameWithoutExtension(fileName);
        if (fallback != null)
            font.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };

        return font;
    }

    /// <summary>
    /// 런타임에 생성한 글꼴 에셋과 아틀라스 텍스처, 머티리얼을 삭제합니다.
    /// </summary>
    private static void DestroyFontAsset(TMP_FontAsset font, bool immediate)
    {
        if (font == null)
            return;

        var targets = new List<Object>();
        if (font.atlasTextures != null)
            targets.AddRange(font.atlasTextures.Where(t => t != null));
        if (font.material != null)
            targets.Add(font.material);
        targets.Add(font);

        foreach (var target in targets)
        {
            if (immediate)
                DestroyImmediate(target);
            else
                Destroy(target);
        }
    }
    #endregion

    #region state
    /// <summary>
    /// 런타임 상태를 초기화합니다. 로드된 리소스는 해제하지 않습니다.
    /// </summary>
    private void ResetRuntimeState()
    {
        fonts = new();
        texts = new();
        loadedFontKeys.Clear();
        externalFonts.Clear();
        isChanging = false;
        IsReady = false;
    }

    /// <summary>
    /// 로드한 Addressable 글꼴과 외부 글꼴을 모두 해제하고 상태를 초기화합니다.
    /// </summary>
    private void ReleaseRuntimeResources()
    {
        foreach (var key in loadedFontKeys)
            AddressableManager.Instance.ReleaseKey(key);
        foreach (var font in externalFonts)
            DestroyFontAsset(font, true);

        ResetRuntimeState();
    }
    #endregion

#if UNITY_EDITOR
    #region editor
    private void OnValidate()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            QueueEditorPreview();
    }

    /// <summary>
    /// Play 진입 전 에디터 미리보기 상태를 비우고, Play 종료 시 런타임 리소스를 해제한 뒤 미리보기를 복원합니다.
    /// </summary>
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.ExitingEditMode:
                ResetRuntimeState();
                break;
            case PlayModeStateChange.ExitingPlayMode:
                ReleaseRuntimeResources();
                break;
            case PlayModeStateChange.EnteredEditMode:
                QueueEditorPreview();
                break;
        }
    }

    /// <summary>
    /// OnValidate/OnEnable 중 에셋 접근을 피하기 위해 다음 에디터 업데이트에 미리보기를 갱신합니다.
    /// </summary>
    private void QueueEditorPreview()
    {
        EditorApplication.delayCall -= RefreshEditorPreview;
        EditorApplication.delayCall += RefreshEditorPreview;
    }

    /// <summary>
    /// 미리보기 언어의 글꼴과 텍스트를 에디터에서 동기로 불러와 적용합니다. JSON 수정 후에는 직접 눌러 갱신합니다.
    /// </summary>
    [Button("Refresh")]
    private void RefreshEditorPreview()
    {
        if (this == null || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var newTexts = new Dictionary<string, string>();
        var newIdEntries = new List<(string file, string id, string text)>();
        var dropdownKeys = new HashSet<string>();
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            var entries = new List<AddressableAssetEntry>();
            settings.GetAllAssets(entries, false, null, e => e.labels.Contains(AddressableLabels.LocalizedText));
            foreach (var entry in entries)
            {
                if (entry.MainAsset is not TextAsset textAsset)
                    continue;

                var fileTexts = new Dictionary<string, string>();
                var fileEntries = new List<(string id, string text)>();
                ParseTexts(textAsset.text, previewLanguage, fileTexts, fileEntries);

                // 같은 파일 안의 중복은 ParseTexts에서 경고하므로 여기서는 파일 간 중복만 검사
                foreach (var (key, text) in fileTexts)
                {
                    if (newTexts.ContainsKey(key))
                        Debug.LogWarning($"[LocalizedDataSO] 여러 파일에 중복된 id: {key} ({textAsset.name})");

                    newTexts[key] = text;
                }

                // 드롭다운에는 원래 표기로 id당 하나씩만 표시
                foreach (var (id, text) in fileEntries)
                    if (dropdownKeys.Add(NormalizeId(id)))
                        newIdEntries.Add((textAsset.name, id, text));
            }
        }

        var newFonts = new Dictionary<FontType, TMP_FontAsset>();
        foreach (FontType type in Enum.GetValues(typeof(FontType)))
        {
            var font = GetFontReference(previewLanguage, type)?.editorAsset;
            if (font != null)
                newFonts[type] = font;
        }

        texts = newTexts;
        fonts = newFonts;
        editorIdEntries = newIdEntries;
        currentLanguage = previewLanguage;
        IsReady = true;

        OnLanguageChangedEvent?.Invoke();
    }

    /// <summary>
    /// 인스펙터 id 드롭다운 목록을 반환합니다. "파일명/id — 미리보기 텍스트" 형태로 파일별로 묶습니다.
    /// </summary>
    public ValueDropdownList<string> GetIdDropdown()
    {
        var list = new ValueDropdownList<string>();
        foreach (var (file, id, text) in editorIdEntries)
            list.Add($"{file}/{id}\t — \t{ToDropdownLabel(text)}", id);
        return list;
    }

    /// <summary>
    /// 드롭다운 표시용으로 텍스트를 한 줄로 줄입니다. '/'는 Odin이 하위 메뉴로 해석하므로 비슷한 문자로 바꿉니다.
    /// </summary>
    private static string ToDropdownLabel(string text)
    {
        const int maxLength = 20;
        text = text.Replace('/', '∕').Replace("\r", "").Replace('\n', ' ');
        return text.Length > maxLength ? text.Substring(0, maxLength) + "…" : text;
    }
    #endregion
#endif
}
