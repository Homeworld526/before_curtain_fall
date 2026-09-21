using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystem;

public class DialogKeywordDetector : MonoBehaviour
{
    [Serializable]
    public class KeywordUnlock
    {
        public string keyword;          // 要检测的关键词
        public string unlockKey;        // 存档键名（与关键词相同）
        public string description;      // 描述（用于调试，一般是打工名称）
        public bool hasTriggered;       // 是否已触发
    }

    /// <summary>
    /// 角色关键词配置
    /// </summary>
    [Serializable]
    public class CharacterKeywordConfig
    {
        public string characterName;       // 角色名
        public List<string> keywords;      // 关键词列表
        public int triggeredCount;         // 已触发的关键词数量
    }

    [Header("剧情解锁关键词列表（自动生成，无需手动配置）")]
    public KeywordUnlock[] keywordUnlocks;

    [Header("角色关键词配置文件路径")]
    public string charKeyConfigPath = "Configs/CharKey";

    [Header("角色关键词配置列表（自动加载）")]
    public List<CharacterKeywordConfig> characterKeywordConfigs = new List<CharacterKeywordConfig>();

    private static Dictionary<string, bool> _unlockedKeywords = new Dictionary<string, bool>();
    public static Dictionary<string, bool> UnlockedKeywords => _unlockedKeywords;

    private Dictionary<string, HashSet<string>> _triggeredCharKeywords = new Dictionary<string, HashSet<string>>();

    private DialogVisual dialogVisual;

    public static event Action<string, bool> OnKeywordUnlocked;

    /// <summary>
    /// 角色关键词触发事件：参数为角色名和触发次数
    /// </summary>
    public static event Action<string, int> OnCharacterKeywordTriggered;

    /// <summary>
    /// 装饰点亮事件：参数为角色名和装饰索引（从0开始）
    /// </summary>
    public static event Action<string, int> OnDecorationLit;

    /// <summary>
    /// 单例实例
    /// </summary>
    public static DialogKeywordDetector Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 等待 ConfigManager 加载完成后再初始化
        StartCoroutine(InitializeAfterConfigLoaded());
    }

    private System.Collections.IEnumerator InitializeAfterConfigLoaded()
    {
        // 等待一帧确保 ConfigManager 已经初始化
        yield return null;
        
        // 自动从 ConfigManager 收集所有打工的解锁关键词
        AutoCollectKeywordsFromConfig();
        
        // 加载角色关键词配置
        LoadCharacterKeywordConfig();
        
        // 加载已触发的状态
        LoadTriggeredState();
        LoadCharacterKeywordTriggeredState();
    }

    private void AutoCollectKeywordsFromConfig()
    {
        List<KeywordUnlock> collectedKeywords = new List<KeywordUnlock>();

        if (ConfigManager.Instance != null)
        {
            // 从普通打工作业收集
            foreach (var job in ConfigManager.Instance.normalJobs)
            {
                if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
                {
                    KeywordUnlock unlock = new KeywordUnlock
                    {
                        keyword = job.unlockDialogKeyword,
                        unlockKey = job.unlockDialogKeyword, // 直接使用关键词作为解锁键
                        description = job.name,
                        hasTriggered = false
                    };
                    collectedKeywords.Add(unlock);
                    Debug.Log($"[AutoCollect] 收集到普通打工关键词: {job.name} -> {job.unlockDialogKeyword}");
                }
            }

            // 从角色打工作业收集
            foreach (var job in ConfigManager.Instance.characterJobs)
            {
                if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
                {
                    KeywordUnlock unlock = new KeywordUnlock
                    {
                        keyword = job.unlockDialogKeyword,
                        unlockKey = job.unlockDialogKeyword, // 直接使用关键词作为解锁键
                        description = job.name,
                        hasTriggered = false
                    };
                    collectedKeywords.Add(unlock);
                    Debug.Log($"[AutoCollect] 收集到角色打工关键词: {job.name} -> {job.unlockDialogKeyword}");
                }
            }
        }
        else
        {
            Debug.LogWarning("[AutoCollect] ConfigManager.Instance 为空，无法自动收集关键词");
        }

        // 更新关键字列表
        keywordUnlocks = collectedKeywords.ToArray();
        Debug.Log($"[AutoCollect] 共收集到 {keywordUnlocks.Length} 个剧情解锁关键词");
    }

    private void Start()
    {
        dialogVisual = FindObjectOfType<DialogVisual>();
        if (dialogVisual != null)
        {
            dialogVisual.ContentTracker += OnDialogContent;
            Debug.Log("[DialogKeywordDetector] 已订阅对话内容事件");
        }
        else
        {
            Debug.LogWarning("[DialogKeywordDetector] 未找到 DialogVisual");
        }
    }

    private void OnDestroy()
    {
        if (dialogVisual != null)
        {
            dialogVisual.ContentTracker -= OnDialogContent;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// 注意：不清空静态事件，因为订阅者是实例对象，会在 OnDestroy 中自动取消订阅
    /// </summary>
    public static void ResetStaticState()
    {
        // 清空静态字典
        if (_unlockedKeywords != null)
        {
            _unlockedKeywords.Clear();
        }
        // 不清空静态事件，保留订阅关系
        // OnKeywordUnlocked = null;
        // OnCharacterKeywordTriggered = null;
        // OnDecorationLit = null;
        Debug.Log("[DialogKeywordDetector] 静态状态已重置（保留事件订阅）");
    }

    private void OnDialogContent(DialogNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.content))
            return;

        string content = node.content;

        // 检测打工解锁关键词
        foreach (var unlock in keywordUnlocks)
        {
            if (unlock.hasTriggered)
                continue;

            if (content.Contains(unlock.keyword))
            {
                Debug.Log($"[DialogKeywordDetector] 检测到关键词: {unlock.keyword}");
                Debug.Log($"[DialogKeywordDetector] 解锁: {unlock.description}");

                SetKeywordUnlocked(unlock.unlockKey, true);
                unlock.hasTriggered = true;

                RefreshJobList();
            }
        }

        // 检测角色关键词
        CheckCharacterKeywords(content);
    }

    /// <summary>
    /// 检测角色关键词
    /// </summary>
    private void CheckCharacterKeywords(string content)
    {
        foreach (var config in characterKeywordConfigs)
        {
            if (!_triggeredCharKeywords.ContainsKey(config.characterName))
            {
                _triggeredCharKeywords[config.characterName] = new HashSet<string>();
            }

            HashSet<string> triggeredSet = _triggeredCharKeywords[config.characterName];

            foreach (string keyword in config.keywords)
            {
                if (triggeredSet.Contains(keyword))
                    continue;

                if (content.Contains(keyword))
                {
                    TriggerCharacterKeyword(config.characterName, keyword);
                }
            }
        }
    }

    /// <summary>
    /// 触发角色关键词
    /// </summary>
    private void TriggerCharacterKeyword(string characterName, string keyword)
    {
        if (!_triggeredCharKeywords.ContainsKey(characterName))
        {
            _triggeredCharKeywords[characterName] = new HashSet<string>();
        }

        _triggeredCharKeywords[characterName].Add(keyword);

        // 计算触发次数
        int triggeredCount = _triggeredCharKeywords[characterName].Count;

        // 更新配置列表中的触发次数
        var config = characterKeywordConfigs.Find(c => c.characterName == characterName);
        if (config != null)
        {
            config.triggeredCount = triggeredCount;
        }

        // 保存触发状态
        SaveCharacterKeywordTriggeredState(characterName, keyword);

        Debug.Log($"[DialogKeywordDetector] 角色 {characterName} 触发关键词: {keyword}, 累计触发次数: {triggeredCount}");

        // 触发事件
        OnCharacterKeywordTriggered?.Invoke(characterName, triggeredCount);

        // 点亮装饰（装饰索引 = 触发次数 - 1）
        int decorationIndex = triggeredCount - 1;
        OnDecorationLit?.Invoke(characterName, decorationIndex);
    }

    public static void SetKeywordUnlocked(string key, bool unlocked)
    {
        if (string.IsNullOrEmpty(key)) return;
        
        _unlockedKeywords[key] = unlocked;
        OnKeywordUnlocked?.Invoke(key, unlocked);
    }

    public static bool IsKeywordUnlocked(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        return _unlockedKeywords.TryGetValue(key, out bool unlocked) && unlocked;
    }

    public static Dictionary<string, bool> GetAllUnlockedKeywords()
    {
        return new Dictionary<string, bool>(_unlockedKeywords);
    }

    public static void LoadUnlockedKeywords(Dictionary<string, bool> keywords)
    {
        _unlockedKeywords.Clear();
        if (keywords != null)
        {
            foreach (var kvp in keywords)
            {
                _unlockedKeywords[kvp.Key] = kvp.Value;
            }
        }
        
        // 同步更新实例中的 hasTriggered 状态
        if (Instance != null && Instance.keywordUnlocks != null)
        {
            foreach (var unlock in Instance.keywordUnlocks)
            {
                unlock.hasTriggered = IsKeywordUnlocked(unlock.unlockKey);
            }
            Debug.Log($"[DialogKeywordDetector] 已同步更新 {Instance.keywordUnlocks.Length} 个关键词的 hasTriggered 状态");
        }
    }

    public static void ClearAllUnlockedKeywords()
    {
        _unlockedKeywords.Clear();
    }

    private void LoadTriggeredState()
    {
        foreach (var unlock in keywordUnlocks)
        {
            unlock.hasTriggered = IsKeywordUnlocked(unlock.unlockKey);
        }
    }

    private void RefreshJobList()
    {
        if (JobManager.Instance != null)
        {
            JobManager.Instance.RefreshCurrentJobList();
            Debug.Log("[DialogKeywordDetector] 已刷新打工列表");
        }
        else if (JobListManager.Instance != null)
        {
            JobListManager.Instance.RefreshAll();
            Debug.Log("[DialogKeywordDetector] 已刷新打工列表(备用方案)");
        }
    }

    [ContextMenu("重置所有关键词状态")]
    private void ResetAllKeywords()
    {
        foreach (var unlock in keywordUnlocks)
        {
            SetKeywordUnlocked(unlock.unlockKey, false);
            unlock.hasTriggered = false;
        }
        RefreshJobList();
        Debug.Log("[DialogKeywordDetector] 已重置所有关键词状态");
    }

    [ContextMenu("触发所有关键词")]
    private void TriggerAllKeywords()
    {
        foreach (var unlock in keywordUnlocks)
        {
            SetKeywordUnlocked(unlock.unlockKey, true);
            unlock.hasTriggered = true;
        }
        RefreshJobList();
        Debug.Log("[DialogKeywordDetector] 已触发所有关键词");
    }

    [ContextMenu("重新收集关键词")]
    private void RecollectKeywords()
    {
        AutoCollectKeywordsFromConfig();
        LoadCharacterKeywordConfig();
        LoadTriggeredState();
        LoadCharacterKeywordTriggeredState();
        Debug.Log("[DialogKeywordDetector] 已重新收集关键词");
    }

    #region 角色关键词配置加载与状态管理

    /// <summary>
    /// 加载角色关键词配置文件
    /// </summary>
    private void LoadCharacterKeywordConfig()
    {
        characterKeywordConfigs.Clear();
        _triggeredCharKeywords.Clear();

        TextAsset configAsset = Resources.Load<TextAsset>(charKeyConfigPath);

        if (configAsset != null)
        {
            ParseCharacterKeywordConfig(configAsset.text);
            Debug.Log($"[DialogKeywordDetector] 加载角色关键词配置成功，共 {characterKeywordConfigs.Count} 个角色");
        }
        else
        {
            Debug.LogWarning($"[DialogKeywordDetector] 未找到角色关键词配置文件: {charKeyConfigPath}");
        }
    }

    /// <summary>
    /// 解析角色关键词配置
    /// 格式：角色名,关键词1,关键词2,...
    /// </summary>
    private void ParseCharacterKeywordConfig(string content)
    {
        string[] lines = content.Split('\n');

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
                continue;

            string[] parts = trimmedLine.Split(',');
            if (parts.Length < 2)
                continue;

            string characterName = parts[0].Trim();

            CharacterKeywordConfig config = new CharacterKeywordConfig
            {
                characterName = characterName,
                keywords = new List<string>(),
                triggeredCount = 0
            };

            // 解析关键词（从第二个元素开始）
            for (int i = 1; i < parts.Length; i++)
            {
                string keyword = parts[i].Trim();
                if (!string.IsNullOrEmpty(keyword))
                {
                    config.keywords.Add(keyword);
                }
            }

            if (config.keywords.Count > 0)
            {
                characterKeywordConfigs.Add(config);
                _triggeredCharKeywords[characterName] = new HashSet<string>();
                Debug.Log($"[DialogKeywordDetector] 角色 {characterName} 配置了 {config.keywords.Count} 个关键词");
            }
        }
    }

    /// <summary>
    /// 加载角色关键词触发状态
    /// </summary>
    private void LoadCharacterKeywordTriggeredState()
    {
        // 不从 PlayerPrefs 加载，状态只在内存中保存
        foreach (var config in characterKeywordConfigs)
        {
            if (!_triggeredCharKeywords.ContainsKey(config.characterName))
            {
                _triggeredCharKeywords[config.characterName] = new HashSet<string>();
            }

            // 初始状态为空，不加载任何已触发状态
            config.triggeredCount = 0;
        }

        Debug.Log("[DialogKeywordDetector] 角色关键词状态初始化完成（不从存档加载）");
    }

    /// <summary>
    /// 保存角色关键词触发状态（已移除自动保存到 PlayerPrefs）
    /// 状态只在内存中保存，重启后重置
    /// </summary>
    private void SaveCharacterKeywordTriggeredState(string characterName, string keyword)
    {
        // 不保存到 PlayerPrefs，状态只在内存中
        // 如果需要存档，应通过存档系统统一保存
    }

    /// <summary>
    /// 获取角色关键词的 PlayerPrefs 键名
    /// </summary>
    private string GetCharacterKeywordPrefsKey(string characterName, string keyword)
    {
        return $"CharKeyword_{characterName}_{keyword}";
    }

    /// <summary>
    /// 获取角色的触发次数
    /// </summary>
    public static int GetCharacterTriggeredCount(string characterName)
    {
        if (Instance == null || Instance._triggeredCharKeywords == null)
            return 0;

        if (Instance._triggeredCharKeywords.TryGetValue(characterName, out HashSet<string> triggeredSet))
        {
            return triggeredSet.Count;
        }
        return 0;
    }

    /// <summary>
    /// 获取角色的关键词总数
    /// </summary>
    public static int GetCharacterTotalKeywordCount(string characterName)
    {
        if (Instance == null || Instance.characterKeywordConfigs == null)
            return 0;

        var config = Instance.characterKeywordConfigs.Find(c => c.characterName == characterName);
        return config != null ? config.keywords.Count : 0;
    }

    /// <summary>
    /// 重置角色关键词状态
    /// </summary>
    [ContextMenu("重置角色关键词状态")]
    private void ResetCharacterKeywords()
    {
        foreach (var config in characterKeywordConfigs)
        {
            config.triggeredCount = 0;
        }

        foreach (var kvp in _triggeredCharKeywords)
        {
            kvp.Value.Clear();
        }

        Debug.Log("[DialogKeywordDetector] 已重置角色关键词状态");
    }

    /// <summary>
    /// 触发所有角色关键词（测试用）
    /// </summary>
    [ContextMenu("触发所有角色关键词")]
    private void TriggerAllCharacterKeywords()
    {
        foreach (var config in characterKeywordConfigs)
        {
            foreach (string keyword in config.keywords)
            {
                if (!_triggeredCharKeywords[config.characterName].Contains(keyword))
                {
                    TriggerCharacterKeyword(config.characterName, keyword);
                }
            }
        }
        Debug.Log("[DialogKeywordDetector] 已触发所有角色关键词");
    }

    /// <summary>
    /// 获取所有角色触发关键词数据（用于存档）
    /// </summary>
    public static Dictionary<string, HashSet<string>> GetAllTriggeredCharKeywords()
    {
        if (Instance == null || Instance._triggeredCharKeywords == null)
            return new Dictionary<string, HashSet<string>>();
        
        return Instance._triggeredCharKeywords;
    }

    /// <summary>
    /// 加载角色触发关键词数据（用于读档）
    /// </summary>
    public static void LoadTriggeredCharKeywords(Dictionary<string, HashSet<string>> data)
    {
        if (Instance == null || Instance._triggeredCharKeywords == null)
            return;
        
        Instance._triggeredCharKeywords.Clear();
        foreach (var kvp in data)
        {
            Instance._triggeredCharKeywords[kvp.Key] = new HashSet<string>(kvp.Value);
        }
        
        // 更新 characterKeywordConfigs 中的 triggeredCount
        foreach (var config in Instance.characterKeywordConfigs)
        {
            if (Instance._triggeredCharKeywords.TryGetValue(config.characterName, out HashSet<string> triggeredSet))
            {
                config.triggeredCount = triggeredSet.Count;
            }
            else
            {
                config.triggeredCount = 0;
            }
        }
        
        Debug.Log($"[DialogKeywordDetector] 加载了 {Instance._triggeredCharKeywords.Count} 个角色的触发关键词数据");
    }

    #endregion
}
