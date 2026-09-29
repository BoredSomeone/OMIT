using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "SettingManagerSO", menuName = "Scriptable Objects/SettingManagerSO")]
public class SettingManagerSO : ScriptableObject
{
    public struct SettingTitle
    {
        public const string Language = "Language";

        public const string Sound = "Sound";
        public const string Master = "Master";
        public const string BGM = "BGM";
        public const string Effect = "Effect";

        public const string Resolution = "Resolution";
        public const string Width = "Width";
        public const string Height = "Height";
        public const string Rate = "Rate";
        public const string FullScreen = "FullScreen";
    }
    public enum SettingType
    {
        Language,

        Master,
        BGM,
        Effect,

        Width,
        Height,
        Rate,
        FullScreen,
    }

    #region field
    // SettingType → ini 섹션 / 키 / 기본값 (Language는 문자열이라 별도 처리)
    static readonly Dictionary<SettingType, (string section, string key, int defaultValue)> ValueTable = new()
    {
        { SettingType.Master,     (SettingTitle.Sound,      SettingTitle.Master,     70) },
        { SettingType.BGM,        (SettingTitle.Sound,      SettingTitle.BGM,        50) },
        { SettingType.Effect,     (SettingTitle.Sound,      SettingTitle.Effect,     70) },

        { SettingType.Width,      (SettingTitle.Resolution, SettingTitle.Width,    1280) },
        { SettingType.Height,     (SettingTitle.Resolution, SettingTitle.Height,    720) },
        { SettingType.Rate,       (SettingTitle.Resolution, SettingTitle.Rate,       60) },
        { SettingType.FullScreen, (SettingTitle.Resolution, SettingTitle.FullScreen,  1) },
    };

    [Title("언어")]
    [SerializeField] SystemLanguage nowLanguage;

    [Title("설정값")]
    [DictionaryDrawerSettings(KeyLabel = "타입", ValueLabel = "값"), ShowInInspector, ReadOnly]
    Dictionary<SettingType, int> settingValues = new();

    IniFile iniFile;

    string FilePath => GlobalValue.SettingsFilePath;

    public SystemLanguage NowLanguage => nowLanguage;
    #endregion

    /// <summary>
    /// ini 파일을 읽어 설정값을 불러옵니다. 파일이 없으면 기본값으로 새로 생성합니다.
    /// 
    /// 없는 항목이나 잘못된 값은 해당 항목만 기본값으로 대체됩니다.
    /// </summary>
    [Button("Load")]
    public void Init()
    {
        iniFile = new IniFile();

        bool isExist = File.Exists(FilePath);
        if (isExist)
        {
            try
            {
                iniFile.Load(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"설정 파일을 읽지 못해 기본값을 사용합니다.\t{FilePath}\n{e.Message}");
                iniFile = new IniFile();
                isExist = false;
            }
        }

        string language = iniFile[SettingTitle.Language][SettingTitle.Language].GetString();
        if (!Enum.TryParse(language, out nowLanguage) || !Enum.IsDefined(typeof(SystemLanguage), nowLanguage))
            nowLanguage = Application.systemLanguage;

        settingValues.Clear();
        foreach (var (type, info) in ValueTable)
            settingValues[type] = iniFile[info.section][info.key].ToInt(info.defaultValue);

        if (!isExist)
            SaveSettings();
    }

    /// <summary>
    /// 현재 설정값을 ini 파일에 저장합니다.
    /// </summary>
    [Button("Save")]
    public void SaveSettings()
    {
        if (iniFile == null)
            iniFile = new IniFile();

        iniFile[SettingTitle.Language][SettingTitle.Language] = nowLanguage.ToString();

        foreach (var (type, info) in ValueTable)
            iniFile[info.section][info.key] = settingValues[type];

        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        iniFile.Save(FilePath);
    }

    /// <summary>
    /// 설정값을 가져옵니다. 불러오지 않은 항목은 기본값을 반환합니다.
    /// </summary>
    public int GetValue(SettingType type)
    {
        if (settingValues.TryGetValue(type, out int value))
            return value;

        if (ValueTable.TryGetValue(type, out var info))
            return info.defaultValue;

        Debug.LogWarning($"{type}은(는) 정수형 설정값이 아닙니다.");
        return 0;
    }

    /// <summary>
    /// 설정값을 변경합니다. 파일에 반영하려면 SaveSettings()를 호출해야 합니다.
    /// </summary>
    public void SetValue(SettingType type, int value)
    {
        if (!ValueTable.ContainsKey(type))
        {
            Debug.LogWarning($"{type}은(는) 정수형 설정값이 아닙니다.");
            return;
        }

        settingValues[type] = value;
    }

    /// <summary>
    /// 언어를 변경합니다. 파일에 반영하려면 SaveSettings()를 호출해야 합니다.
    /// </summary>
    public void SetLanguage(SystemLanguage language)
    {
        nowLanguage = language;
    }
}
