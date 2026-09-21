using System;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "SettingsSO", menuName = "Settings/Settings SO", order = 1)]
public class SettingsSO : ScriptableObject
{
    [Serializable]
    private class SettingsData
    {
        public float masterVolume;
        public float musicVolume;
        public float sfxVolume;
        public float printSpeed;
        public float autoPlaySpeed;
        public bool onlySkipReadDialog;
    }

    public float MasterVolume;
    public float MusicVolume;
    public float SFXVolume;
    public float PrintSpeed;
    public float AutoPlaySpeed = 0.5f;
    public bool OnlySkipReadDialog;

    private string SavePath => Path.Combine(Application.persistentDataPath, "Settings.json");

    public void LoadFromDisk()
    {
        if (!File.Exists(SavePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            SettingsData data = JsonUtility.FromJson<SettingsData>(json);
            if (data == null)
            {
                return;
            }

            MasterVolume = data.masterVolume;
            MusicVolume = data.musicVolume;
            SFXVolume = data.sfxVolume;
            PrintSpeed = data.printSpeed;
            AutoPlaySpeed = data.autoPlaySpeed;
            OnlySkipReadDialog = data.onlySkipReadDialog;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SettingsSO] 读取设置失败: {e.Message}");
        }
    }

    public void SaveToDisk()
    {
        try
        {
            SettingsData data = new SettingsData
            {
                masterVolume = MasterVolume,
                musicVolume = MusicVolume,
                sfxVolume = SFXVolume,
                printSpeed = PrintSpeed,
                autoPlaySpeed = AutoPlaySpeed,
                onlySkipReadDialog = OnlySkipReadDialog
            };
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SettingsSO] 保存设置失败: {e.Message}");
        }
    }
}
