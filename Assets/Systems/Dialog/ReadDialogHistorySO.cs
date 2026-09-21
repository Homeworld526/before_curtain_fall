using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "ReadDialogHistorySO", menuName = "Dialog/Read Dialog History", order = 2)]
public class ReadDialogHistorySO : ScriptableObject
{
    [Serializable]
    public class DialogReadEntry
    {
        public int dialogIndex;
        public int lineIndex;
    }

    [Serializable]
    private class DialogReadEntryList
    {
        public List<DialogReadEntry> entries = new List<DialogReadEntry>();
    }

    [SerializeField] private List<DialogReadEntry> entries = new List<DialogReadEntry>();

    private readonly HashSet<string> _entrySet = new HashSet<string>();
    private bool _loaded;

    private string SavePath => Path.Combine(Application.persistentDataPath, "ReadDialogHistory.json");

    private void OnEnable()
    {
        RebuildCache();
    }

    public bool HasShown(int dialogIndex, int lineIndex)
    {
        EnsureLoaded();
        return _entrySet.Contains(GetKey(dialogIndex, lineIndex));
    }

    public void MarkAsShown(int dialogIndex, int lineIndex)
    {
        EnsureLoaded();

        string key = GetKey(dialogIndex, lineIndex);
        if (_entrySet.Contains(key))
        {
            return;
        }

        entries.Add(new DialogReadEntry
        {
            dialogIndex = dialogIndex,
            lineIndex = lineIndex
        });
        _entrySet.Add(key);
        SaveToDisk();
    }

    public void ClearHistory()
    {
        entries.Clear();
        _entrySet.Clear();
        _loaded = true;
        SaveToDisk();
    }

    private void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        LoadFromDisk();
        _loaded = true;
    }

    private void LoadFromDisk()
    {
        if (!File.Exists(SavePath))
        {
            RebuildCache();
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            DialogReadEntryList data = JsonUtility.FromJson<DialogReadEntryList>(json);
            entries = data?.entries ?? new List<DialogReadEntry>();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ReadDialogHistorySO] 读取已读对话记录失败: {e.Message}");
            entries = new List<DialogReadEntry>();
        }

        RebuildCache();
    }

    private void SaveToDisk()
    {
        try
        {
            DialogReadEntryList data = new DialogReadEntryList
            {
                entries = entries
            };
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ReadDialogHistorySO] 保存已读对话记录失败: {e.Message}");
        }
    }

    private void RebuildCache()
    {
        if (entries == null)
        {
            entries = new List<DialogReadEntry>();
        }

        _entrySet.Clear();
        foreach (var entry in entries)
        {
            _entrySet.Add(GetKey(entry.dialogIndex, entry.lineIndex));
        }
    }

    private static string GetKey(int dialogIndex, int lineIndex)
    {
        return $"{dialogIndex}:{lineIndex}";
    }
}
