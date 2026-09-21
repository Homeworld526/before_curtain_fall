using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogStatEntry
{
    public string key;
    public int value;
}

[CreateAssetMenu(fileName = "DialogStatData", menuName = "Dialog/Stat Data")]
public class DialogStatDataSO : ScriptableObject
{
    private readonly Dictionary<string, int> _stats = new Dictionary<string, int>();

    public int GetInt(string key, int defaultValue = 0)
    {
        return _stats.TryGetValue(key, out int v) ? v : defaultValue;
    }

    public void SetInt(string key, int value)
    {
        _stats[key] = value;
    }

    public List<DialogStatEntry> GetAllEntries()
    {
        var list = new List<DialogStatEntry>();
        foreach (var kvp in _stats)
            list.Add(new DialogStatEntry { key = kvp.Key, value = kvp.Value });
        return list;
    }

    public void LoadFromEntries(List<DialogStatEntry> entries)
    {
        _stats.Clear();
        if (entries == null) return;
        foreach (var e in entries)
            _stats[e.key] = e.value;
    }

    public void Clear()
    {
        _stats.Clear();
    }
}
