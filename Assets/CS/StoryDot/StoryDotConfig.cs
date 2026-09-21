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
    }

    [Tooltip("角色名称")]
    public string characterName;

    [Tooltip("该角色的剧情列表（按顺序解锁）")]
    public StoryEntry[] stories;
}
