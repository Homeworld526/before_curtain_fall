using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum ReturnHomeTriggerSource
{
    None = 0,
    WeeklySettlementClosed = 1 << 0,
    DialogEnded = 1 << 1,
    All = WeeklySettlementClosed | DialogEnded,
}

[CreateAssetMenu(fileName = "ReturnHomeStoryConfig", menuName = "Story/Return Home Story Config")]
public sealed class ReturnHomeStoryConfig : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [Serializable]
        public sealed class ResourceCondition
        {
            [Tooltip("要检测的游戏内资源数值。")]
            public GameResourceType resourceType;

            public bool requireMinimum;
            public long minimum;
            public bool requireMaximum;
            public long maximum;

            public bool IsEligible()
            {
                long currentValue = resourceType.GetCurrentValue();
                if (requireMinimum && currentValue < minimum)
                    return false;
                if (requireMaximum && currentValue > maximum)
                    return false;

                return true;
            }
        }

        [Tooltip("用于日志和配置校验的唯一标识。")]
        public string id;

        public bool enabled = true;

        [Tooltip("数值越大优先级越高；相同优先级按当前列表顺序。")]
        public int priority;

        [Tooltip("可触发检测的回家入口。")]
        public ReturnHomeTriggerSource triggerSources = ReturnHomeTriggerSource.All;

        [Tooltip("要启动的结局配置 ID。前置剧情表和结局 CG 均由结局配置管理。")]
        public string endingId;

        [Tooltip("启用后，同一存档只会触发一次。")]
        public bool triggerOnce = true;

        [Tooltip("一次性触发标记，保存在 DialogStatDataSO 中。")]
        public string triggeredFlag;

        public bool requireMinimumWeek;
        [Min(1)] public int minimumWeek = 1;
        public bool requireMaximumWeek;
        [Min(1)] public int maximumWeek = 1;

        [Tooltip("月份和日期同时为 0 表示不限制日期。")]
        [Range(0, 12)] public int requiredMonth;
        [Range(0, 31)] public int requiredDay;

        [Tooltip("所有已启用的资源条件都必须满足。")]
        public List<ResourceCondition> resourceConditions = new List<ResourceCondition>();

        public bool Supports(ReturnHomeTriggerSource source)
        {
            return (triggerSources & source) != 0;
        }

        public bool IsEligible(DialogStatDataSO statData)
        {
            if (triggerOnce && statData.GetInt(triggeredFlag, 0) != 0)
                return false;

            JobManager jobManager = RequireJobManagerIfNeeded();
            if (requireMinimumWeek && jobManager.currentWeek < minimumWeek)
                return false;
            if (requireMaximumWeek && jobManager.currentWeek > maximumWeek)
                return false;

            if (requiredMonth != 0)
            {
                DateTime currentDate = jobManager.CurrentGameDate;
                if (currentDate.Month != requiredMonth || currentDate.Day != requiredDay)
                    return false;
            }

            foreach (ResourceCondition condition in resourceConditions)
            {
                if (!condition.IsEligible())
                    return false;
            }

            return true;
        }

        private JobManager RequireJobManagerIfNeeded()
        {
            bool needsJobManager = requireMinimumWeek || requireMaximumWeek ||
                requiredMonth != 0 || HasResourceCondition(GameResourceType.Money);
            if (!needsJobManager)
                return null;

            if (JobManager.Instance == null)
                throw new InvalidOperationException($"回家自动剧情“{id}”的周数、日期或现金条件需要活动的 JobManager 实例。");

            return JobManager.Instance;
        }

        private bool HasResourceCondition(GameResourceType resourceType)
        {
            foreach (ResourceCondition condition in resourceConditions)
            {
                if (condition.resourceType == resourceType)
                    return true;
            }

            return false;
        }
    }

    public List<Entry> entries = new List<Entry>();

    public void ValidateEntries()
    {
        if (entries == null)
            throw new InvalidOperationException($"回家自动剧情配置 {name} 的 entries 不能为空。");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var triggeredFlags = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < entries.Count; index++)
        {
            Entry entry = entries[index];
            if (entry == null)
                throw new InvalidOperationException($"回家自动剧情配置 {name} 的第 {index + 1} 条规则为空。");
            if (string.IsNullOrWhiteSpace(entry.id))
                throw new InvalidOperationException($"回家自动剧情配置 {name} 的第 {index + 1} 条规则缺少 id。");
            if (!ids.Add(entry.id))
                throw new InvalidOperationException($"回家自动剧情配置 {name} 包含重复 id：{entry.id}。");
            if (entry.triggerSources == ReturnHomeTriggerSource.None)
                throw new InvalidOperationException($"回家自动剧情“{entry.id}”未选择任何触发入口。");
            if (string.IsNullOrWhiteSpace(entry.endingId))
                throw new InvalidOperationException($"回家自动剧情“{entry.id}”未配置结局 ID。");
            if (!EndingService.Contains(entry.endingId))
            {
                throw new InvalidOperationException(
                    $"回家自动剧情“{entry.id}”引用了不存在的结局 ID：{entry.endingId}。");
            }
            if (entry.triggerOnce)
            {
                if (string.IsNullOrWhiteSpace(entry.triggeredFlag))
                    throw new InvalidOperationException($"一次性回家自动剧情“{entry.id}”缺少触发标记。");
                if (!triggeredFlags.Add(entry.triggeredFlag))
                    throw new InvalidOperationException($"回家自动剧情配置 {name} 重复使用触发标记：{entry.triggeredFlag}。");
            }

            ValidateRange(entry.id, "周数", entry.requireMinimumWeek, entry.minimumWeek,
                entry.requireMaximumWeek, entry.maximumWeek);
            ValidateResourceConditions(entry);

            bool hasMonth = entry.requiredMonth != 0;
            bool hasDay = entry.requiredDay != 0;
            if (hasMonth != hasDay)
                throw new InvalidOperationException($"回家自动剧情“{entry.id}”的日期必须同时填写月份和日期，或同时留空。");
            if (hasMonth)
            {
                try
                {
                    new DateTime(2024, entry.requiredMonth, entry.requiredDay);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new InvalidOperationException(
                        $"回家自动剧情“{entry.id}”的日期无效：{entry.requiredMonth} 月 {entry.requiredDay} 日。");
                }
            }

        }
    }

    private static void ValidateResourceConditions(Entry entry)
    {
        if (entry.resourceConditions == null)
        {
            throw new InvalidOperationException($"回家自动剧情“{entry.id}”的资源条件列表不能为空。");
        }

        var resourceTypes = new HashSet<GameResourceType>();
        foreach (Entry.ResourceCondition condition in entry.resourceConditions)
        {
            if (condition == null)
                throw new InvalidOperationException($"回家自动剧情“{entry.id}”包含空的资源条件。");
            if (!Enum.IsDefined(typeof(GameResourceType), condition.resourceType))
            {
                throw new InvalidOperationException(
                    $"回家自动剧情“{entry.id}”包含未定义的资源类型：{(int)condition.resourceType}。");
            }
            if (!resourceTypes.Add(condition.resourceType))
            {
                throw new InvalidOperationException(
                    $"回家自动剧情“{entry.id}”重复配置了资源条件：{condition.resourceType}。");
            }
            if (!condition.requireMinimum && !condition.requireMaximum)
            {
                throw new InvalidOperationException(
                    $"回家自动剧情“{entry.id}”的资源条件 {condition.resourceType} 未启用最低值或最高值。");
            }

            ValidateRange(entry.id, condition.resourceType.ToString(), condition.requireMinimum, condition.minimum,
                condition.requireMaximum, condition.maximum);
        }
    }

    private static void ValidateRange<T>(string id, string label, bool hasMinimum, T minimum,
        bool hasMaximum, T maximum) where T : IComparable<T>
    {
        if (hasMinimum && hasMaximum && minimum.CompareTo(maximum) > 0)
            throw new InvalidOperationException($"回家自动剧情“{id}”的{label}范围无效：最低值不能大于最高值。");
    }
}

public static class ReturnHomeStoryService
{
    private const string ResourcePath = "ReturnHomeStories/ReturnHomeStoryConfig";
    private static ReturnHomeStoryConfig _config;
    private static bool _automaticStoryActive;
    private static string _activeStoryId;

    public static void ResetForNewGame(DialogStatDataSO statData)
    {
        if (statData == null)
            throw new ArgumentNullException(nameof(statData));

        ReturnHomeStoryConfig config = GetConfig();
        config.ValidateEntries();
        foreach (ReturnHomeStoryConfig.Entry entry in config.entries)
        {
            if (entry.triggerOnce)
                statData.SetInt(entry.triggeredFlag, 0);
        }

        EndingService.ResetForNewGame(statData);

        _automaticStoryActive = false;
        _activeStoryId = null;
    }

    public static bool CompleteAutomaticStory()
    {
        if (!_automaticStoryActive)
            return false;

        Debug.Log($"[ReturnHomeStory] 自动剧情“{_activeStoryId}”已结束，本次回家不再重复检测。");
        _automaticStoryActive = false;
        _activeStoryId = null;
        return true;
    }

    public static void ResetRuntimeState()
    {
        _automaticStoryActive = false;
        _activeStoryId = null;
    }

    public static bool TryStartForReturnHome(ReturnHomeTriggerSource source)
    {
        if (_automaticStoryActive)
            return false;

        ReturnHomeStoryConfig config = GetConfig();
        config.ValidateEntries();
        DialogList dialogList = RequireDialogList();
        ReturnHomeStoryConfig.Entry selected = SelectEntry(config, dialogList.statData, source);
        if (selected == null)
            return false;

        if (selected.triggerOnce)
            dialogList.statData.SetInt(selected.triggeredFlag, 1);

        _automaticStoryActive = true;
        _activeStoryId = selected.id;
        if (!EndingService.TryStart(selected.endingId))
            throw new InvalidOperationException($"回家自动剧情“{selected.id}”引用了不存在的结局 ID：{selected.endingId}。");

        if (!EndingService.IsAwaitingStoryCompletion)
            CompleteAutomaticStory();

        Debug.Log($"[ReturnHomeStory] {source} 触发结局“{selected.endingId}”。");
        return true;
    }

    private static ReturnHomeStoryConfig.Entry SelectEntry(ReturnHomeStoryConfig config, DialogStatDataSO statData,
        ReturnHomeTriggerSource source)
    {
        ReturnHomeStoryConfig.Entry selected = null;
        foreach (ReturnHomeStoryConfig.Entry entry in config.entries)
        {
            if (!entry.enabled || !entry.Supports(source) || !entry.IsEligible(statData))
                continue;

            if (selected == null || entry.priority > selected.priority)
                selected = entry;
        }

        return selected;
    }

    private static DialogList RequireDialogList()
    {
        if (DialogList.Instance == null)
            throw new InvalidOperationException("回家自动剧情需要活动的 DialogList 实例。");
        if (DialogList.Instance.statData == null)
            throw new InvalidOperationException("回家自动剧情需要 DialogList 配置 DialogStatDataSO。");
        if (DialogList.Instance.dialogs == null)
            throw new InvalidOperationException("回家自动剧情需要 DialogList.dialogs 配置。");

        return DialogList.Instance;
    }

    private static ReturnHomeStoryConfig GetConfig()
    {
        if (_config != null)
            return _config;

        _config = Resources.Load<ReturnHomeStoryConfig>(ResourcePath);
        if (_config == null)
        {
            throw new InvalidOperationException(
                $"缺少回家自动剧情配置 Resources/{ResourcePath}.asset。请恢复或创建该配置资产。");
        }

        return _config;
    }
}
