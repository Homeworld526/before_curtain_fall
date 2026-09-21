using System;
using UnityEngine;

namespace GameSystem
{
    // 新增：一周的七天
    [Serializable]
    public enum DayOfWeek { Mon, Tue, Wed, Thu, Fri, Sat, Sun }
    [Serializable]
    public enum LuckType { Lucky, Normal, Unlucky }

    // 更新：一天三个时段
    [Serializable]
    public enum TimeSlot { Morning, Afternoon, Night }

    // 更新：工作可用的时间段类型
    [Serializable]
    public enum TimeType
    {
        AllDay,         // 全天可用
        DayOnly,        // 上午、下午可用
        NightOnly,      // 仅晚上可用
        SundayOnly,     // 仅周日全天
        CharOnly,       // 角色线特殊逻辑
        MorningOnly,    // (可选扩展) 仅上午
        AfternoonOnly   // (可选扩展) 仅下午
    }

    [Serializable]
    public class JobData
    {
        public bool ischar = false;
        public string id;
        public string name;
        public int apCost;
        public TimeType timeType;
        public int cdVal;
        public int curcd = 0;
        public int[] rewards;         // 报酬数组 (长度应为3: Lv1, Lv2, Lv3)
        public string[] Refs;
        public int unlockWeek;
        public int lockChapter;
        public string unlockDialogKeyword;  // 解锁需要的剧情关键词
        public string unlockCharacterName;  // 关联角色名称
        public int favorIncrement;
        public int Backbone;
        public string imageName;
        public bool isEmpty = false;
        public bool cannotBeRemoved = false; // 是否不能被删除
        public string head;
        public string BG;
        public string BG_1;
        public string BG_2;
        public string npcName;
        public string Description;


        // --- 运行时数据 (非序列化) ---
        [NonSerialized] public int instanceId;
        [NonSerialized] public int currentCd;

        // 当前等级 (0=初级, 1=中级, 2=高级)
        [NonSerialized] public int currentLevel = 0;

        [NonSerialized] public int exp = 0;

        [NonSerialized] public int performedCountThisWeek;

        [NonSerialized] public bool hasClickedCue = false;

        [NonSerialized] public int plannedAffectionLevel = -1;

        public bool CanPerformAt(DayOfWeek day, TimeSlot slot)
        {
            switch (timeType)
            {
                case TimeType.AllDay: return true;
                case TimeType.DayOnly: return slot != TimeSlot.Night;
                case TimeType.NightOnly: return slot == TimeSlot.Night;
                case TimeType.SundayOnly: return day == DayOfWeek.Sun;
                case TimeType.CharOnly: return true;
                default: return true;
            }
        }
    }
}
