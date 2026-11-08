using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StoryDotConfig", menuName = "StoryDot/Story Dot Config")]
public class StoryDotConfig : ScriptableObject
{
    [System.Serializable]
    public class StoryEntry
    {
        [Tooltip("剧情解锁所需周数")]
        public int requiredWeek;
        [Tooltip("对应的对话索引（DialogList.dialogs 中的索引）")]
        public int dialogIndex;
        [Tooltip("好感度需求（0=无需求）")]
        public int requiredAffection;
        [Tooltip("前置对话内容（每行一句）")]
        [TextArea(3, 10)]
        public string preDialogContent = "";

        [Tooltip("是否启用骨气最低值条件。")]
        public bool requireMinimumBackbone;

        [Tooltip("骨气最低值。可填写任意整数。")]
        public int minimumBackbone;

        [Tooltip("是否启用骨气最高开区间条件。")]
        public bool requireMaximumBackboneExclusive;

        [Tooltip("骨气最高值（开区间）。例如 40 表示骨气必须低于 40；可填写任意整数。")]
        public int maximumBackboneExclusive;

        [Tooltip("限定月份（0=不限制）。生日剧情填写 12。")]
        [Range(0, 12)] public int requiredMonth;

        [Tooltip("限定日期（0=不限制）。填写月份时必须同时填写日期。")]
        [Range(0, 31)] public int requiredDay;
    }

    [Tooltip("角色名称")]
    public string characterName;

    [Tooltip("该角色的剧情列表（按顺序解锁）")]
    public StoryEntry[] stories;

    public void ValidateStoryEntries()
    {
        if (stories == null)
            throw new InvalidOperationException($"剧情点配置 {name} 的 stories 不能为空。");

        for (int index = 0; index < stories.Length; index++)
        {
            StoryEntry entry = stories[index];
            if (entry == null)
                throw new InvalidOperationException($"剧情点配置 {name} 的第 {index + 1} 条剧情为空。");

            if (entry.requireMinimumBackbone && entry.requireMaximumBackboneExclusive &&
                entry.minimumBackbone >= entry.maximumBackboneExclusive)
            {
                throw new InvalidOperationException(
                    $"剧情点配置 {name} 的第 {index + 1} 条骨气范围无效：最低值必须小于最高开区间。");
            }

            bool hasMonth = entry.requiredMonth != 0;
            bool hasDay = entry.requiredDay != 0;
            if (hasMonth != hasDay)
            {
                throw new InvalidOperationException(
                    $"剧情点配置 {name} 的第 {index + 1} 条日期必须同时填写月份和日期，或同时留空。");
            }

            if (hasMonth)
            {
                try
                {
                    new DateTime(2024, entry.requiredMonth, entry.requiredDay);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new InvalidOperationException(
                        $"剧情点配置 {name} 的第 {index + 1} 条日期无效：{entry.requiredMonth} 月 {entry.requiredDay} 日。");
                }
            }
        }
    }
}
