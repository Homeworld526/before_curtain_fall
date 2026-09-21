using System.Collections.Generic;
using UnityEngine;

public class PrologueUIManager : MonoBehaviour
{
    public static PrologueUIManager Instance;

    [Header("序章结束判定")]
    [Tooltip("序章对话在 DialogList 中的索引")]
    public int prologueDialogIndex = 1;

    [Tooltip("检测的关键词（当这句对话播放完才算序章结束）")]
    public string endKeyword = "还债吧";

    [Tooltip("是否需要等待对话完全结束才触发教学")]
    public bool waitForDialogEnd = false;

    // 已触发的关键词集合（用于避免重复触发）
    private HashSet<string> _triggeredKeywords = new HashSet<string>();

    private NormalEndEvent normalEndEvent;
    private DialogVisual dialogVisual;
    private static bool isPrologueCompleted = false;

    /// <summary>序章是否已完成（供外部判断）</summary>
    public static bool IsPrologueCompleted => isPrologueCompleted;
    /// <summary>设置序章为已完成（供存档系统使用）</summary>
    public static void SetPrologueCompleted() => isPrologueCompleted = true;
    private bool hasReachedEndKeyword = false;

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void EditorReset()
    {
        isPrologueCompleted = false;
    }
#endif

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
    }

    private void Start()
    {
        LoadKeywordTriggeredState();
        SubscribeToEvents();
    }

    private void LoadKeywordTriggeredState()
    {
        if (JobManager.Instance == null) return;

        foreach (var jobKVP in JobManager.Instance._allJobs)
        {
            var job = jobKVP.Value;
            if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
            {
                if (DialogKeywordDetector.IsKeywordUnlocked(job.unlockDialogKeyword))
                {
                    _triggeredKeywords.Add(job.unlockDialogKeyword);
                }
            }
        }

        foreach (var jobKVP in JobManager.Instance._allcharJobs)
        {
            var job = jobKVP.Value;
            if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
            {
                if (DialogKeywordDetector.IsKeywordUnlocked(job.unlockDialogKeyword))
                {
                    _triggeredKeywords.Add(job.unlockDialogKeyword);
                }
            }
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void SubscribeToEvents()
    {
        normalEndEvent = FindObjectOfType<NormalEndEvent>();
        if (normalEndEvent != null)
        {
            normalEndEvent.DialogEnded += OnDialogEnded;
            Debug.Log("[PrologueUIManager] 订阅了 DialogEnded 事件");
        }
        else
        {
            Debug.LogWarning("[PrologueUIManager] 找不到 NormalEndEvent 组件！");
        }

        dialogVisual = FindObjectOfType<DialogVisual>();
        if (dialogVisual != null)
        {
            dialogVisual.ContentTracker += OnDialogContentShown;
            Debug.Log("[PrologueUIManager] 订阅了 ContentTracker 事件");
        }
        else
        {
            Debug.LogWarning("[PrologueUIManager] 找不到 DialogVisual 组件！");
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (normalEndEvent != null)
        {
            normalEndEvent.DialogEnded -= OnDialogEnded;
        }

        if (dialogVisual != null)
        {
            dialogVisual.ContentTracker -= OnDialogContentShown;
        }
    }

    private void OnDialogContentShown(DialogNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.content)) return;

        CheckJobUnlockKeywords(node.content);

        if (isPrologueCompleted) return;
        if (DialogList.CurrentIndex != prologueDialogIndex) return;

        if (node.content.Contains(endKeyword))
        {
            hasReachedEndKeyword = true;
            Debug.Log($"[PrologueUIManager] 检测到序章结束关键词: {endKeyword}");

            if (!waitForDialogEnd)
            {
                CompletePrologue();
            }
        }
    }

    private void CheckJobUnlockKeywords(string content)
    {
        if (JobManager.Instance == null) return;

        string cleanContent = StripColorTags(content);

        bool hasNewKeywordUnlocked = false;

        foreach (var jobKVP in JobManager.Instance._allJobs)
        {
            var job = jobKVP.Value;
            if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
            {
                // 只使用 DialogKeywordDetector 的状态来判断是否已解锁（存档会保存这个状态）
                // 不再使用 _triggeredKeywords，因为它不会在读档时被清空
                bool alreadyUnlocked = DialogKeywordDetector.IsKeywordUnlocked(job.unlockDialogKeyword);
                
                if (!alreadyUnlocked && cleanContent.Contains(job.unlockDialogKeyword))
                {
                    Debug.Log($"[PrologueUIManager] 检测到打工解锁关键词: {job.unlockDialogKeyword}，解锁打工: {job.name}");
                    DialogKeywordDetector.SetKeywordUnlocked(job.unlockDialogKeyword, true);
                    hasNewKeywordUnlocked = true;
                }
            }
        }

        foreach (var jobKVP in JobManager.Instance._allcharJobs)
        {
            var job = jobKVP.Value;
            if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
            {
                // 只使用 DialogKeywordDetector 的状态来判断是否已解锁（存档会保存这个状态）
                bool alreadyUnlocked = DialogKeywordDetector.IsKeywordUnlocked(job.unlockDialogKeyword);
                
                if (!alreadyUnlocked && cleanContent.Contains(job.unlockDialogKeyword))
                {
                    Debug.Log($"[PrologueUIManager] 检测到打工解锁关键词: {job.unlockDialogKeyword}，解锁打工: {job.name}");
                    DialogKeywordDetector.SetKeywordUnlocked(job.unlockDialogKeyword, true);
                    hasNewKeywordUnlocked = true;
                }
            }
        }

        if (hasNewKeywordUnlocked)
        {
            RefreshJobList();
        }
    }

    private string StripColorTags(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return System.Text.RegularExpressions.Regex.Replace(text, @"<color=[^>]+>[^<]*</color>", "");
    }

    private void RefreshJobList()
    {
        if (JobListManager.Instance != null)
        {
            JobListManager.Instance.RefreshAll();
            Debug.Log("[PrologueUIManager] 已刷新打工列表");
        }
    }

    private void OnDialogEnded()
    {
        if (isPrologueCompleted) return;
        if (!hasReachedEndKeyword) return;

        Debug.Log("[PrologueUIManager] 序章对话结束");
        CompletePrologue();
    }

    public void CompletePrologue()
    {
        isPrologueCompleted = true;

        // 序章结束后立即刷新感叹号状态（不需要打开地图）
        if (ShowMapButton.instance != null)
        {
            ShowMapButton.instance.InitIndicators();
        }

        Debug.Log("[教学] 尝试弹出教学, TutorialManager.Instance=" + (TutorialManager.Instance != null));
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ShowTutorialUI();
        }

        UnsubscribeFromEvents();

        Debug.Log("[PrologueUIManager] 序章正式结束");
    }

    [ContextMenu("重置所有打工解锁关键词")]
    public void ResetAllKeywordTriggers()
    {
        if (JobManager.Instance == null) return;

        foreach (var jobKVP in JobManager.Instance._allJobs)
        {
            var job = jobKVP.Value;
            if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
            {
                DialogKeywordDetector.SetKeywordUnlocked(job.unlockDialogKeyword, false);
            }
        }

        foreach (var jobKVP in JobManager.Instance._allcharJobs)
        {
            var job = jobKVP.Value;
            if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
            {
                DialogKeywordDetector.SetKeywordUnlocked(job.unlockDialogKeyword, false);
            }
        }

        _triggeredKeywords.Clear();
        RefreshJobList();
        Debug.Log("[PrologueUIManager] 已重置所有打工解锁关键词");
    }

    [ContextMenu("触发所有打工解锁关键词（测试用）")]
    public void TriggerAllKeywordUnlocks()
    {
        if (JobManager.Instance == null) return;

        foreach (var jobKVP in JobManager.Instance._allJobs)
        {
            var job = jobKVP.Value;
            if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
            {
                DialogKeywordDetector.SetKeywordUnlocked(job.unlockDialogKeyword, true);
                _triggeredKeywords.Add(job.unlockDialogKeyword);
            }
        }

        foreach (var jobKVP in JobManager.Instance._allcharJobs)
        {
            var job = jobKVP.Value;
            if (!string.IsNullOrEmpty(job.unlockDialogKeyword))
            {
                DialogKeywordDetector.SetKeywordUnlocked(job.unlockDialogKeyword, true);
                _triggeredKeywords.Add(job.unlockDialogKeyword);
            }
        }

        RefreshJobList();
        Debug.Log("[PrologueUIManager] 已触发所有打工解锁关键词");
    }

    private void OnApplicationQuit()
    {
        isPrologueCompleted = false;
        hasReachedEndKeyword = false;
    }

    public void ResetForNewGame()
    {
        isPrologueCompleted = false;
        hasReachedEndKeyword = false;
        _triggeredKeywords.Clear();
    }

    /// <summary>
    /// 静态重置序章完成标志（供新游戏时在 Instance 可能为 null 的场合调用）
    /// </summary>
    public static void ResetPrologueCompleted()
    {
        isPrologueCompleted = false;
    }
}
