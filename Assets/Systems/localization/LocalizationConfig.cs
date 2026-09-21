using System;
using System.IO;
using UnityEngine;

public enum GameLanguage { Chinese, English }

[Serializable]
public class DialogCsvEntry
{
    public string key;
    public TextAsset asset;
}

[Serializable]
public class LanguageDialogMapping
{
    public GameLanguage language;
    public DialogCsvEntry[] dialogAssets;
}

[CreateAssetMenu(menuName = "Localization/LocalizationConfig")]
public class LocalizationConfig : ScriptableObject
{
    public LanguageDialogMapping[] languages;
    public GameLanguage currentLanguage = GameLanguage.Chinese;

    [Serializable]
    private class SaveData { public string language; }

    private string SavePath => Path.Combine(Application.persistentDataPath, "localization_settings.json");

    public void SaveSettings()
    {
        File.WriteAllText(SavePath, JsonUtility.ToJson(new SaveData { language = currentLanguage.ToString() }));
    }

    public GameLanguage LoadSettings()
    {
        if (File.Exists(SavePath))
        {
            var raw = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(raw);
            if (Enum.TryParse(data.language, out GameLanguage lang))
                currentLanguage = lang;
        }
        return currentLanguage;
    }

    public TextAsset GetDialogAsset(string key, GameLanguage lang)
    {
        foreach (var mapping in languages)
        {
            if (mapping.language != lang) continue;
            foreach (var entry in mapping.dialogAssets)
                if (entry.key == key) return entry.asset;
        }
        return null;
    }
}
