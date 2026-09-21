using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryDotManager : MonoBehaviour
{
    private static StoryDotManager _instance;
    public static StoryDotManager Instance
    {
        get
        {
            // 检查 fake null（对象已被销毁但引用不为 null）
            if (_instance == null || !_instance)
                _instance = FindObjectOfType<StoryDotManager>();
            return _instance;
        }
        private set => _instance = value;
    }

    public event Action<string> OnStoryCompleted;
    public event Action OnStoryStateChanged;

    // 内存记录已完成的剧情（格式：characterName_dialogIndex）
    private static readonly HashSet<string> _completedStories = new HashSet<string>();

    // 直接追踪配置，不依赖指示器的生命周期
    private readonly HashSet<StoryDotConfig> _registeredConfigs = new HashSet<StoryDotConfig>();

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void EditorReset()
    {
        _completedStories.Clear();
    }
#endif

    private void Awake()
    {
        // _instance 可能被 Instance getter 的 FindObjectOfType 提前设置为 this
        // （例如 StoryDotIndicator.OnEnable 在本对象 Awake 之前执行时）
        // 此时不能误判为"已存在其他实例"而自我销毁
        if (_instance == null || !_instance || _instance == this)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            // 好感度变化时刷新感叹号状态
            AffectionManager.OnAffectionChanged += OnAffectionChanged;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnAffectionChanged(string characterId)
    {
        NotifyStateChanged();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _registeredConfigs.Clear();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null; // 清空静态变量
            SceneManager.sceneLoaded -= OnSceneLoaded;
            AffectionManager.OnAffectionChanged -= OnAffectionChanged;
        }
    }

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// DontDestroyOnLoad 对象在场景切换时不会被销毁，所以保留 Instance、事件订阅和已注册配置。
    /// _registeredConfigs 由 OnSceneLoaded 在场景切换时自动清空，不需要在这里处理。
    /// </summary>
    public static void ResetStaticState()
    {
        // 不设置 _instance = null，不清空 _registeredConfigs：
        // - 同场景读档：旧对象仍然有效，Instance 和配置都需保持可用
        // - 场景切换：旧对象 DontDestroyOnLoad 保持，新场景的 StoryDotManager
        //   会因 _instance 非空而自动销毁自己（保持单例）；
        //   OnSceneLoaded 回调会自动清空 _registeredConfigs
        Debug.Log("[StoryDotManager] 静态状态已重置（保留 Instance、事件订阅和已注册配置）");
    }

    public static bool IsStoryCompleted(string characterName, int dialogIndex)
    {
        return _completedStories.Contains(characterName + "_" + dialogIndex);
    }

    public bool IsStoryAvailable(StoryDotConfig config)
    {
        if (config == null || config.stories == null) return false;
        int currentWeek = JobManager.Instance != null ? JobManager.Instance.currentWeek : 1;
        foreach (var entry in config.stories)
        {
            if (!IsStoryCompleted(config.characterName, entry.dialogIndex)
                && currentWeek >= entry.requiredWeek
                && CheckAffection(config.characterName, entry.requiredAffection))
                return true;
        }

        return false;
    }

    public int GetFirstAvailableDialogIndex(StoryDotConfig config)
    {
        if (config == null || config.stories == null) return -1;
        int currentWeek = JobManager.Instance != null ? JobManager.Instance.currentWeek : 1;
        foreach (var entry in config.stories)
        {
            if (!IsStoryCompleted(config.characterName, entry.dialogIndex)
                && currentWeek >= entry.requiredWeek
                && CheckAffection(config.characterName, entry.requiredAffection))
                return entry.dialogIndex;
        }
        return -1;
    }

    private static bool CheckAffection(string characterName, int required)
    {
        if (required <= 0) return true;
        if (AffectionManager.Instance == null) return true;
        return AffectionManager.Instance.GetAffection(characterName) >= required;
    }

    public void MarkCompleted(string characterName, int dialogIndex)
    {
        string key = characterName + "_" + dialogIndex;
        if (_completedStories.Contains(key)) return;
        _completedStories.Add(key);
        OnStoryCompleted?.Invoke(characterName);
        NotifyStateChanged();
        Debug.Log($"[StoryDotManager] 剧情完成: {characterName} dialogIndex={dialogIndex}");
    }

    public void RegisterConfig(StoryDotConfig config)
    {
        if (config != null && _registeredConfigs.Add(config))
            NotifyStateChanged();
    }

    /// <summary>
    /// 批量注册配置，不触发事件（由调用方最后统一通知一次）
    /// </summary>
    public void BatchRegisterConfigs(StoryDotIndicator[] indicators)
    {
        if (indicators == null) return;
        foreach (var ind in indicators)
        {
            if (ind != null && ind.config != null)
                _registeredConfigs.Add(ind.config);
        }
    }

    public void UnregisterConfig(StoryDotConfig config)
    {
        if (config != null && _registeredConfigs.Remove(config))
            NotifyStateChanged();
    }

    private bool _notifying = false;

    /// <summary>
    /// 通知所有监听者状态已变化（防重入，避免多个指示器互相触发无限递归）
    /// </summary>
    public void NotifyStateChanged()
    {
        if (_notifying) return;
        _notifying = true;
        OnStoryStateChanged?.Invoke();
        _notifying = false;
    }

    public bool HasAnyAvailable()
    {
        foreach (var config in _registeredConfigs)
        {
            if (config != null && IsStoryAvailable(config))
                return true;
        }
        return false;
    }

    public bool HasAnyAvailableForCharacter(string characterName)
    {
        foreach (var config in _registeredConfigs)
        {
            if (config != null && config.characterName == characterName && IsStoryAvailable(config))
                return true;
        }
        return false;
    }

    public List<StoryDotConfig> GetAllStoryDots()
    {
        return new List<StoryDotConfig>(_registeredConfigs);
    }

    public void ResetForNewGame()
    {
        _completedStories.Clear();
        NotifyStateChanged();
    }

    // --- 存档接口 ---
    public List<string> GetCompletedStoryIds()
    {
        return new List<string>(_completedStories);
    }

    public void LoadCompletedStoryIds(List<string> ids)
    {
        _completedStories.Clear();
        if (ids != null)
        {
            foreach (string id in ids)
                _completedStories.Add(id);
        }
        NotifyStateChanged();
    }

    public static void LoadCompletedStoryIdsStatic(List<string> ids)
    {
        _completedStories.Clear();
        if (ids != null)
        {
            foreach (string id in ids)
                _completedStories.Add(id);
        }
        Instance?.NotifyStateChanged();
    }
}
