using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Demo 剧情完成进度追踪器
/// 在 Inspector 中配置所有需要体验的剧情 key，
/// 每段剧情结束后调用 MarkComplete 标记，全部完成时触发 Demo 结束流程。
/// </summary>
public class DemoProgressTracker : MonoBehaviour
{
    public static DemoProgressTracker Instance { get; private set; }

    [Header("需要完成的剧情列表")]
    [Tooltip("在这里填写所有需要体验的剧情 key，每段剧情的 DemoEndTrigger 中填写对应的 key")]
    public List<string> requiredStorylines = new List<string>();

    [Header("调试")]
    [SerializeField] private bool logProgress = true;

    private const string SAVE_PREFIX = "DemoProgress_";

    [SerializeField] private GameObject prefab;
    [SerializeField] private Canvas targetLayer;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 标记某段剧情已完成，然后检查是否全部完成
    /// </summary>
    public void MarkComplete(string storylineKey)
    {
        if (string.IsNullOrEmpty(storylineKey)) return;

        PlayerPrefs.SetInt(SAVE_PREFIX + storylineKey, 1);
        PlayerPrefs.Save();

        if (logProgress)
        {
            int done = GetCompletedCount();
            Debug.Log($"[DemoProgressTracker] 剧情 \"{storylineKey}\" 已完成 ({done}/{requiredStorylines.Count})");
        }

        // 检查是否全部完成
        if (IsAllComplete())
        {
            Debug.Log("[DemoProgressTracker] 所有剧情已完成，触发 Demo 结束");
            if (DemoCreditsOverlay.Instance == null && prefab != null && targetLayer != null)
            {
                Instantiate(prefab, targetLayer.transform);
            }
            DemoCreditsOverlay.Instance?.Play();
        }
    }

    /// <summary>
    /// 检查某段剧情是否已完成
    /// </summary>
    public bool IsComplete(string storylineKey)
    {
        return PlayerPrefs.GetInt(SAVE_PREFIX + storylineKey, 0) == 1;
    }

    /// <summary>
    /// 检查是否所有剧情都已完成
    /// </summary>
    public bool IsAllComplete()
    {
        if (requiredStorylines.Count == 0) return false;

        foreach (var key in requiredStorylines)
        {
            if (PlayerPrefs.GetInt(SAVE_PREFIX + key, 0) == 0)
                return false;
        }
        return true;
    }

    /// <summary>
    /// 获取已完成数量
    /// </summary>
    public int GetCompletedCount()
    {
        int count = 0;
        foreach (var key in requiredStorylines)
        {
            if (PlayerPrefs.GetInt(SAVE_PREFIX + key, 0) == 1)
                count++;
        }
        return count;
    }

    /// <summary>
    /// 触发 Demo 结束（第 8 周到期时调用，根据剧情完成情况选择版本）
    /// </summary>
    public void TriggerEnd()
    {
        bool allDone = IsAllComplete();
        int creditsShown = PlayerPrefs.GetInt("DemoCreditsShown", 0);
        Debug.Log($"[DemoProgressTracker] 触发 Demo 结束，剧情全部完成: {allDone}, DemoCreditsShown: {creditsShown}, 已完成: {GetCompletedCount()}/{requiredStorylines.Count}");

        if (DemoCreditsOverlay.Instance == null && prefab != null && targetLayer != null)
        {
            Instantiate(prefab, targetLayer.transform);
        }

        if (DemoCreditsOverlay.Instance == null)
        {
            Debug.LogError("[DemoProgressTracker] DemoCreditsOverlay 实例化失败");
            return;
        }

        if (allDone)
        {
            DemoCreditsOverlay.Instance.Play();
        }
        else
        {
            DemoCreditsOverlay.Instance.PlayIncomplete();
        }
    }

    // --- 存档接口 ---

    /// <summary>
    /// 获取已完成的剧情 key 列表（用于存档）
    /// </summary>
    public List<string> GetCompletedKeys()
    {
        var list = new List<string>();
        foreach (var key in requiredStorylines)
        {
            if (PlayerPrefs.GetInt(SAVE_PREFIX + key, 0) == 1)
                list.Add(key);
        }
        return list;
    }

    /// <summary>
    /// 从存档恢复已完成的剧情 key（完整替换）
    /// </summary>
    public void LoadCompletedKeys(List<string> keys)
    {
        // 先清空所有
        foreach (var key in requiredStorylines)
        {
            PlayerPrefs.DeleteKey(SAVE_PREFIX + key);
        }
        // 再恢复存档中的
        if (keys != null)
        {
            foreach (var key in keys)
            {
                PlayerPrefs.SetInt(SAVE_PREFIX + key, 1);
            }
        }
        PlayerPrefs.Save();
        Debug.Log($"[DemoProgressTracker] 从存档恢复了 {keys?.Count ?? 0} 个剧情进度");
    }

    /// <summary>
    /// 重置所有进度（用于新游戏或测试）
    /// </summary>
    [ContextMenu("重置所有进度")]
    public void ResetAllProgress()
    {
        foreach (var key in requiredStorylines)
        {
            PlayerPrefs.DeleteKey(SAVE_PREFIX + key);
        }
        PlayerPrefs.DeleteKey("DemoCreditsShown");
        PlayerPrefs.Save();
        Debug.Log("[DemoProgressTracker] 已重置所有剧情进度");
    }

    /// <summary>
    /// 标记所有剧情已完成（测试用）
    /// </summary>
    [ContextMenu("标记全部完成（测试用）")]
    public void MarkAllComplete()
    {
        foreach (var key in requiredStorylines)
        {
            PlayerPrefs.SetInt(SAVE_PREFIX + key, 1);
        }
        PlayerPrefs.Save();
        Debug.Log("[DemoProgressTracker] 已标记所有剧情完成");
        if (IsAllComplete())
        {
            Debug.Log("[DemoProgressTracker] 所有剧情已完成，触发 Demo 结束");
            if (DemoCreditsOverlay.Instance == null && prefab != null && targetLayer != null)
            {
                Instantiate(prefab, targetLayer.transform);
            }
            DemoCreditsOverlay.Instance?.Play();
        }
    }

    private void OnDestroy()
    {
        // 清空静态变量，避免指向已销毁的对象
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
