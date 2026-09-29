using System.IO;
using UnityEngine;

public static class GlobalValue
{
    /// <summary>설정 파일 폴더 경로입니다.</summary>
    public static string SettingsDirectory => Path.Combine(FilePath(), "Settings");

    /// <summary>설정 ini 파일 경로입니다. (SettingManagerSO)</summary>
    public static string SettingsFilePath => Path.Combine(SettingsDirectory, "Settings.ini");

    /// <summary>외부 폰트 파일을 넣는 폴더 경로입니다.</summary>
    public static string ExternalFontDirectory => Path.Combine(SettingsDirectory, "Fonts");

    /// <summary>외부 폰트 지정 ini 파일 경로입니다. (LocalizedDataSO)</summary>
    public static string ExternalFontIniPath => Path.Combine(ExternalFontDirectory, "Fonts.ini");

    /// <summary>
    /// 저장 파일의 기준 경로를 반환합니다.
    /// </summary>
    public static string FilePath()
    {
        if(Application.isEditor)
            return Path.GetDirectoryName(Application.dataPath);

        return Application.persistentDataPath;
    }
}
