using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public class HotKeyManager : MonoBehaviour
{
    [Serializable]
    public class HotKeyEntry
    {
        public string name;
        public List<KeyCode> keys = new List<KeyCode>();
        public UnityEvent onPressed;

        public bool IsPressed()
        {
            if (keys == null || keys.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                if (!Input.GetKey(keys[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < keys.Count; i++)
            {
                if (Input.GetKeyDown(keys[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }

    // --- JSON 序列化结构 ---
    [Serializable]
    private class HotKeyBindData
    {
        public string name;
        public List<string> keyCodes = new List<string>();
    }

    [Serializable]
    private class HotKeyMapData
    {
        public List<HotKeyBindData> bindings = new List<HotKeyBindData>();
    }

    [SerializeField] private List<HotKeyEntry> hotKeys = new List<HotKeyEntry>();

    public event Action<HotKeyEntry> HotKeyPressed;

    public IReadOnlyList<HotKeyEntry> HotKeys => hotKeys;

    private string SavePath => Path.Combine(Application.persistentDataPath, "HotKeys.json");

    // --- 重绑定状态 ---
    public bool IsRebinding { get; private set; }
    private string _rebindTargetName;
    private Action<KeyCode> _rebindCallback;

    private void Awake()
    {
        LoadFromJson();
    }

    private void Update()
    {
        if (IsRebinding)
        {
            HandleRebindInput();
            return;
        }

        for (int i = 0; i < hotKeys.Count; i++)
        {
            HotKeyEntry hotKey = hotKeys[i];
            if (hotKey == null || !hotKey.IsPressed())
            {
                continue;
            }

            hotKey.onPressed?.Invoke();
            HotKeyPressed?.Invoke(hotKey);
        }
    }

    // --- 重绑定 API ---

    public void BeginRebind(string entryName, Action<KeyCode> onComplete)
    {
        _rebindTargetName = entryName;
        _rebindCallback = onComplete;
        IsRebinding = true;
    }

    public void CancelRebind()
    {
        IsRebinding = false;
        _rebindTargetName = null;
        _rebindCallback = null;
    }

    private void HandleRebindInput()
    {
        foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
        {
            if (kc == KeyCode.None) continue;
            // 鼠标键排除
            if (kc >= KeyCode.Mouse0 && kc <= KeyCode.Mouse6) continue;

            if (!Input.GetKeyDown(kc)) continue;

            // Escape：取消重绑定
            if (kc == KeyCode.Escape)
            {
                CancelRebind();
                return;
            }

            // 冲突检查：将其他 entry 中相同按键清除
            foreach (var other in hotKeys)
            {
                if (other == null || other.name == _rebindTargetName) continue;
                if (other.keys != null && other.keys.Contains(kc))
                    other.keys.Clear();
            }

            // 写入目标 entry
            var entry = hotKeys.Find(e => e.name == _rebindTargetName);
            if (entry != null)
            {
                entry.keys.Clear();
                entry.keys.Add(kc);
            }

            SaveToJson();

            var cb = _rebindCallback;
            IsRebinding = false;
            _rebindTargetName = null;
            _rebindCallback = null;
            cb?.Invoke(kc);
            return;
        }
    }

    // --- 持久化 ---

    public void LoadFromJson()
    {
        if (!File.Exists(SavePath)) return;

        try
        {
            string json = File.ReadAllText(SavePath);
            var mapData = JsonUtility.FromJson<HotKeyMapData>(json);
            if (mapData == null || mapData.bindings == null) return;

            foreach (var bind in mapData.bindings)
            {
                var entry = hotKeys.Find(e => e.name == bind.name);
                if (entry == null) continue;

                var parsed = new List<KeyCode>();
                foreach (var kcStr in bind.keyCodes)
                {
                    if (Enum.TryParse<KeyCode>(kcStr, out var kc))
                        parsed.Add(kc);
                    else
                        Debug.LogWarning($"[HotKeyManager] 无法解析 KeyCode: {kcStr}，已跳过");
                }

                if (parsed.Count > 0)
                    entry.keys = parsed;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[HotKeyManager] 读取快捷键配置失败: {e.Message}");
        }
    }

    public void SaveToJson()
    {
        try
        {
            var mapData = new HotKeyMapData();
            foreach (var entry in hotKeys)
            {
                if (entry == null) continue;
                var bind = new HotKeyBindData { name = entry.name };
                foreach (var kc in entry.keys)
                    bind.keyCodes.Add(kc.ToString());
                mapData.bindings.Add(bind);
            }

            File.WriteAllText(SavePath, JsonUtility.ToJson(mapData, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"[HotKeyManager] 保存快捷键配置失败: {e.Message}");
        }
    }
}
