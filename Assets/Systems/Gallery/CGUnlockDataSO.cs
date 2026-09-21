using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "CGUnlockData", menuName = "Gallery/CG Unlock Data", order = 1)]
public class CGUnlockDataSO : ScriptableObject
{
    [Serializable]
    private class UnlockData
    {
        public List<string> unlockedNames = new List<string>();
    }

    private readonly HashSet<string> _unlocked = new HashSet<string>();
    private bool _loaded;

    private string SavePath => Path.Combine(Application.persistentDataPath, "CGUnlockData.json");

    private void OnEnable()
    {
        _loaded = false;
    }

    public void Unlock(string picName)
    {
        EnsureLoaded();
        if (_unlocked.Add(picName))
            SaveToDisk();
    }

    public bool IsUnlocked(string picName)
    {
        EnsureLoaded();
        return _unlocked.Contains(picName);
    }

    public void ClearAll()
    {
        _unlocked.Clear();
        _loaded = true;
        SaveToDisk();
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        LoadFromDisk();
        _loaded = true;
    }

    private void LoadFromDisk()
    {
        if (!File.Exists(SavePath))
            return;

        try
        {
            string json = File.ReadAllText(SavePath);
            UnlockData data = JsonUtility.FromJson<UnlockData>(json);
            _unlocked.Clear();
            if (data?.unlockedNames != null)
                foreach (string n in data.unlockedNames)
                    _unlocked.Add(n);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CGUnlockDataSO] 读取解锁数据失败: {e.Message}");
        }
    }

    private void SaveToDisk()
    {
        try
        {
            UnlockData data = new UnlockData();
            data.unlockedNames.AddRange(_unlocked);
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"[CGUnlockDataSO] 保存解锁数据失败: {e.Message}");
        }
    }
}
