using UnityEngine;

public class StoryDotIndicator : MonoBehaviour
{
    [Tooltip("感叹号 GameObject（作为子物体放在角色头上）")]
    public GameObject exclamationMark;

    [Tooltip("剧情配置")]
    public StoryDotConfig config;

    private void Awake()
    {
        if (exclamationMark != null)
            exclamationMark.GetComponent<CanvasGroup>().alpha =  0f;
    }

    private void OnEnable()
    {
        if (config != null && StoryDotManager.Instance != null)
        {
            StoryDotManager.Instance.RegisterConfig(config);
            StoryDotManager.Instance.OnStoryCompleted += OnStoryCompleted;
            StoryDotManager.Instance.OnStoryStateChanged += Refresh;
        }
        Refresh();
    }

    private void OnDisable()
    {
        // 只取消事件订阅，不注销配置（地图关闭时物体被禁用，但配置应保持注册）
        if (StoryDotManager.Instance != null)
        {
            StoryDotManager.Instance.OnStoryCompleted -= OnStoryCompleted;
            StoryDotManager.Instance.OnStoryStateChanged -= Refresh;
        }
    }

    private void OnDestroy()
    {
        // 物体销毁时才注销配置
        if (config != null && StoryDotManager.Instance != null)
        {
            StoryDotManager.Instance.UnregisterConfig(config);
        }
    }

    private void OnStoryCompleted(string characterName)
    {
        if (config != null && config.characterName == characterName)
            Refresh();
    }

    public void Refresh()
    {
        if (exclamationMark == null || config == null) return;
        bool available = StoryDotManager.Instance != null && StoryDotManager.Instance.IsStoryAvailable(config);
        exclamationMark.GetComponent<CanvasGroup>().alpha = available ? 1f : 0f;

        // 通知地图按钮上的总感叹号刷新（防重入由 StoryDotManager 控制）
        if (StoryDotManager.Instance != null)
            StoryDotManager.Instance.NotifyStateChanged();
    }
}
