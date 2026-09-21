using UnityEngine;

/// <summary>
/// Demo 剧情完成标记器
/// 通过对话索引识别是哪段剧情结束，不占用 CSV 备注列。
/// 挂到场景中任意 GameObject 上即可，不需要加入 DialogList.hosts。
/// </summary>
public class DemoEndTrigger : MonoBehaviour
{
    [Tooltip("对应的剧情 key，需与 DemoProgressTracker.requiredStorylines 中的条目一致")]
    public string storylineKey;

    [Tooltip("对应的对话在 DialogList.dialogs 列表中的索引（从 0 开始）")]
    public int dialogIndex;

    private void OnEnable()
    {
        if (StatEventCenter.Instance != null)
        {
            StatEventCenter.Instance.onDialogEnd += OnDialogEnd;
        }
    }

    private void OnDisable()
    {
        if (StatEventCenter.Instance != null)
        {
            StatEventCenter.Instance.onDialogEnd -= OnDialogEnd;
        }
    }

    private void OnDialogEnd(string param)
    {
        // 用对话索引判断是哪段剧情结束了
        if (DialogList.Instance == null) return;
        if (DialogList.Instance.currentIndex != dialogIndex) return;

        Debug.Log($"[DemoEndTrigger] 剧情 \"{storylineKey}\" 完成 (dialogIndex: {dialogIndex}, param: {param})");
        if (DemoProgressTracker.Instance != null)
        {
            DemoProgressTracker.Instance.MarkComplete(storylineKey);
        }
        else
        {
            Debug.LogWarning("[DemoEndTrigger] DemoProgressTracker.Instance 为 null");
        }
    }
}
