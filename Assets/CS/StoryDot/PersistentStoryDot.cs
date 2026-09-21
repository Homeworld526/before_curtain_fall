using UnityEngine;

/// <summary>
/// 始终可见的剧情感叹号，独立于地图。
/// 挂在场景中（不在地图层级内），地图关闭时也能显示。
/// 放在角色头顶或任意你想要的位置。
/// </summary>
public class PersistentStoryDot : MonoBehaviour
{
    [Tooltip("感叹号物体（会自动显示/隐藏）")]
    public GameObject exclamationMark;

    [Tooltip("对应的剧情配置")]
    public StoryDotConfig config;

    [Tooltip("是否跟随某个世界物体的位置（可选）")]
    public Transform followTarget;

    [Tooltip("相对偏移")]
    public Vector3 offset = new Vector3(0, 1.5f, 0);

    private CanvasGroup _cg;

    private void Awake()
    {
        if (exclamationMark != null)
        {
            _cg = exclamationMark.GetComponent<CanvasGroup>();
            if (_cg == null) _cg = exclamationMark.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;
        }
    }

    private void OnEnable()
    {
        if (config != null && StoryDotManager.Instance != null)
        {
            StoryDotManager.Instance.OnStoryStateChanged += Refresh;
            StoryDotManager.Instance.RegisterConfig(config);
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (config != null && StoryDotManager.Instance != null)
        {
            StoryDotManager.Instance.OnStoryStateChanged -= Refresh;
        }
    }

    private void OnDestroy()
    {
        if (config != null && StoryDotManager.Instance != null)
        {
            StoryDotManager.Instance.UnregisterConfig(config);
        }
    }

    private void Update()
    {
        // 跟随目标位置
        if (followTarget != null)
        {
            transform.position = followTarget.position + offset;
        }
    }

    public void Refresh()
    {
        if (exclamationMark == null || config == null) return;
        bool available = StoryDotManager.Instance != null &&
                         StoryDotManager.Instance.IsStoryAvailable(config);

        if (_cg == null)
        {
            _cg = exclamationMark.GetComponent<CanvasGroup>();
            if (_cg == null) _cg = exclamationMark.AddComponent<CanvasGroup>();
        }

        _cg.alpha = available ? 1f : 0f;
    }
}
