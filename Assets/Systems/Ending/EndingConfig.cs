using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EndingConfig", menuName = "Story/Ending Config")]
public sealed class EndingConfig : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [Tooltip("结局的唯一标识。剧情表 END 的指令参数和其他触发器均使用此值。")]
        public string id;

        [Tooltip("结局展示前要播放的 DialogList.dialogs 索引；填 -1 时直接展示结局 CG。")]
        public int storyDialogIndex = -1;

        [Tooltip("结局 CG 背景图。")]
        public Sprite background;

        public string title;

        [TextArea(3, 8)]
        public string description;
    }

    public List<Entry> entries = new List<Entry>();

    public Entry Find(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        foreach (Entry entry in entries)
        {
            if (entry != null && entry.id == id)
                return entry;
        }

        return null;
    }

    public void ValidateEntries()
    {
        if (entries == null || entries.Count == 0)
            throw new InvalidOperationException($"结局配置 {name} 至少需要一条结局。");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var storyIndexes = new HashSet<int>();
        foreach (Entry entry in entries)
        {
            if (entry == null)
                throw new InvalidOperationException($"结局配置 {name} 包含空条目。");
            if (string.IsNullOrWhiteSpace(entry.id))
                throw new InvalidOperationException($"结局配置 {name} 存在缺少 id 的条目。");
            if (!ids.Add(entry.id))
                throw new InvalidOperationException($"结局配置 {name} 包含重复 id：{entry.id}。");
            if (entry.storyDialogIndex < -1)
                throw new InvalidOperationException($"结局“{entry.id}”的剧情表索引必须为 -1 或有效的非负索引。");
            if (entry.storyDialogIndex >= 0 && !storyIndexes.Add(entry.storyDialogIndex))
            {
                throw new InvalidOperationException(
                    $"结局配置 {name} 有多个结局使用剧情表索引 {entry.storyDialogIndex}，无法在读档后确定结局。");
            }
            if (entry.background == null)
                throw new InvalidOperationException($"结局“{entry.id}”缺少 CG 背景图。");
            if (string.IsNullOrWhiteSpace(entry.title))
                throw new InvalidOperationException($"结局“{entry.id}”缺少标题。");
            if (string.IsNullOrWhiteSpace(entry.description))
                throw new InvalidOperationException($"结局“{entry.id}”缺少描述。");
        }
    }
}

public static class EndingService
{
    private const string ResourcePath = "Endings/EndingConfig";
    private const string ActiveStoryFlagPrefix = "Ending.ActiveStory.";

    private static EndingConfig _config;
    private static EndingConfig.Entry _activeEnding;

    public static bool IsAwaitingStoryCompletion =>
        _activeEnding != null && _activeEnding.storyDialogIndex >= 0;

    public static bool Contains(string id)
    {
        return GetConfig().Find(id) != null;
    }

    public static bool TryStart(string id)
    {
        EndingConfig.Entry entry = GetConfig().Find(id);
        if (entry == null)
            return false;

        if (_activeEnding != null)
            throw new InvalidOperationException($"结局“{_activeEnding.id}”尚未结束，不能启动“{id}”。");

        _activeEnding = entry;
        PlayerPrefs.SetInt("Endings", 1);
        PlayerPrefs.SetInt($"Ending.{entry.id}", 1);
        PlayerPrefs.Save();

        if (entry.storyDialogIndex < 0)
        {
            ShowActiveEnding();
            return true;
        }

        DialogList dialogList = RequireDialogList();
        if (entry.storyDialogIndex >= dialogList.dialogs.Count)
        {
            throw new InvalidOperationException(
                $"结局“{entry.id}”的剧情表索引 {entry.storyDialogIndex} 超出 DialogList.dialogs 范围。");
        }

        GameObject dialog = RequireDialogObject();
        dialogList.statData.SetInt(GetActiveStoryFlag(entry.id), 1);
        dialog.SetActive(true);
        dialogList.InvokeDialog(entry.storyDialogIndex, 0);
        Debug.Log($"[Ending] 开始结局“{entry.id}”的前置剧情，索引 {entry.storyDialogIndex}。");
        return true;
    }

    public static bool TryCompleteActiveStory()
    {
        if (_activeEnding == null && DialogList.Instance != null && DialogList.Instance.statData != null)
            _activeEnding = FindSavedActiveStory(DialogList.Instance);

        if (!IsAwaitingStoryCompletion)
            return false;

        if (DialogList.Instance == null)
            throw new InvalidOperationException("结局前置剧情结束时缺少活动的 DialogList 实例。");
        if (DialogList.Instance.currentIndex != _activeEnding.storyDialogIndex)
        {
            throw new InvalidOperationException(
                $"结局“{_activeEnding.id}”正在等待剧情索引 {_activeEnding.storyDialogIndex} 结束，但当前索引为 {DialogList.Instance.currentIndex}。");
        }

        ShowActiveEnding();
        return true;
    }

    public static void CompletePresentation()
    {
        if (_activeEnding != null && _activeEnding.storyDialogIndex >= 0)
        {
            DialogList dialogList = RequireDialogList();
            dialogList.statData.SetInt(GetActiveStoryFlag(_activeEnding.id), 0);
        }

        _activeEnding = null;
    }

    public static void ResetForNewGame(DialogStatDataSO statData)
    {
        if (statData == null)
            throw new ArgumentNullException(nameof(statData));

        foreach (EndingConfig.Entry entry in GetConfig().entries)
        {
            if (entry.storyDialogIndex >= 0)
                statData.SetInt(GetActiveStoryFlag(entry.id), 0);
        }

        _activeEnding = null;
    }

    public static void ResetRuntimeState()
    {
        _activeEnding = null;
    }

    private static void ShowActiveEnding()
    {
        if (_activeEnding == null)
            throw new InvalidOperationException("没有活动结局可供展示。");
        if (EndingPresentation.Instance == null)
            throw new InvalidOperationException("结局展示需要场景中的 EndingPresentation 组件。");

        EndingPresentation.Instance.Show(_activeEnding);
    }

    private static EndingConfig.Entry FindSavedActiveStory(DialogList dialogList)
    {
        foreach (EndingConfig.Entry entry in GetConfig().entries)
        {
            if (entry.storyDialogIndex == dialogList.currentIndex &&
                dialogList.statData.GetInt(GetActiveStoryFlag(entry.id), 0) != 0)
            {
                return entry;
            }
        }

        return null;
    }

    private static string GetActiveStoryFlag(string endingId)
    {
        return ActiveStoryFlagPrefix + endingId;
    }

    private static DialogList RequireDialogList()
    {
        if (DialogList.Instance == null)
            throw new InvalidOperationException("结局前置剧情需要活动的 DialogList 实例。");
        if (DialogList.Instance.dialogs == null)
            throw new InvalidOperationException("结局前置剧情需要 DialogList.dialogs 配置。");

        return DialogList.Instance;
    }

    private static GameObject RequireDialogObject()
    {
        if (DialogLocator.Instance == null || DialogLocator.Instance.Dialog == null)
            throw new InvalidOperationException("结局前置剧情需要 DialogLocator 配置 Dialog 对象。");

        return DialogLocator.Instance.Dialog;
    }

    private static EndingConfig GetConfig()
    {
        if (_config != null)
            return _config;

        _config = Resources.Load<EndingConfig>(ResourcePath);
        if (_config == null)
            throw new InvalidOperationException($"缺少结局配置 Resources/{ResourcePath}.asset。");

        _config.ValidateEntries();
        return _config;
    }
}
