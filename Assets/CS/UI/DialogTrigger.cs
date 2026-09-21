using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂到地图头像上，通过 StoryDotConfig 驱动剧情。
/// 感叹号可用时点击播放剧情（支持前置对话→确认→播放），否则点击无反应。
/// </summary>
public class DialogTrigger : MonoBehaviour
{
    /// <summary>任意头像被点击时触发</summary>
    public static event System.Action OnAnyAvatarClicked;

    [Header("剧情感叹号")]
    [Tooltip("剧情感叹号配置")]
    public StoryDotConfig storyDotConfig;

    [Tooltip("感叹号指示器组件（场景中角色头上的感叹号）")]
    public StoryDotIndicator storyDotIndicator;

    [Header("前置对话配置")]
    [Tooltip("是否启用前置对话")]
    public bool enablePreDialog = false;

    [Tooltip("场景中的 SimpleDialog 组件")]
    public SimpleDialog simpleDialog;

    [Header("确认UI")]
    [Tooltip("确认/取消 UI 根节点")]
    public GameObject confirmUI;

    [Tooltip("确认按钮")]
    public Button confirmButton;

    [Tooltip("取消按钮")]
    public Button cancelButton;

    [Tooltip("确认UI上的标题文字（加粗加大）")]
    public TMPro.TextMeshProUGUI confirmTitleText;

    [Tooltip("标题内容")]
    [TextArea(1, 2)]
    public string confirmTitle = "前往剧情";

    [Tooltip("确认UI上的描述文字")]
    public TMPro.TextMeshProUGUI confirmDescText;

    [Tooltip("描述内容")]
    [TextArea(1, 3)]
    public string confirmContent = "是否进入剧情？";

    [Header("UI控制")]
    [Tooltip("地图按钮管理器，用于关闭/打开地图")]
    public ShowMapButton showMapButton;

    private RectTransform myRect;
    private Canvas rootCanvas;
    private bool isPlaying = false;

    public void ResetPlaying()
    {
        isPlaying = false;
        // 清空对话索引，防止 BlackoutTransition 残留协程回调用到旧索引启动 B 的对话
        _currentStoryDialogIndex = -1;
        // 取消订阅 DialogEnded，防止残留订阅在读档后被触发导致 MarkCompleted
        if (normalEndEvent != null)
            normalEndEvent.DialogEnded -= OnDialogEnded;
    }
    private NormalEndEvent normalEndEvent;
    private int _currentStoryDialogIndex = -1;

    public static DialogTrigger PlayingTrigger { get; private set; }
    public int CurrentStoryDialogIndex => _currentStoryDialogIndex;

    // 当前正在等待确认的 trigger，确保同一时刻只有一个 trigger 监听确认按钮
    private static DialogTrigger _activeTrigger = null;

    private void Start()
    {
        myRect = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();

        if (confirmUI != null)
        {
            confirmUI.SetActive(false);
        }

        normalEndEvent = FindObjectOfType<NormalEndEvent>();

        if (storyDotIndicator != null)
            storyDotIndicator.Refresh();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (myRect == null || rootCanvas == null) return;
        if (isPlaying) return;

        bool inside = RectTransformUtility.RectangleContainsScreenPoint(
            myRect, Input.mousePosition, rootCanvas.worldCamera);

        if (inside)
        {
            // ★ 先触发事件（引导系统需要监听，不管有没有剧情）
            OnAnyAvatarClicked?.Invoke();

            // 引导期间不进入剧情
            if (TutorialGuideManager.IsGuideActive)
            {
                Debug.Log("[DialogTrigger] 引导期间屏蔽点击, IsGuideActive=true");
                return;
            }

            OnAvatarClicked();
        }
    }

    private void OnAvatarClicked()
    {

        // 没有配置或没有可用剧情时，点击无反应
        if (storyDotConfig == null || StoryDotManager.Instance == null) return;
        if (!StoryDotManager.Instance.IsStoryAvailable(storyDotConfig)) return;

        _currentStoryDialogIndex = StoryDotManager.Instance.GetFirstAvailableDialogIndex(storyDotConfig);
        if (_currentStoryDialogIndex < 0) return;

        // 立刻关闭地图
        if (showMapButton != null && showMapButton.isShow)
        {
            showMapButton.ShowMap();
        }

        if (enablePreDialog && simpleDialog != null)
        {
            var entry = System.Array.Find(storyDotConfig.stories, e => e.dialogIndex == _currentStoryDialogIndex);
            if (entry != null && !string.IsNullOrEmpty(entry.preDialogContent))
            {
                RegisterConfirmListeners();
                string[] lines = entry.preDialogContent.Split('\n');
                simpleDialog.Show(lines, OnPreDialogComplete);
                return;
            }
        }
        PlayMainDialog();
    }

    private void RegisterConfirmListeners()
    {
        if (_activeTrigger != null && _activeTrigger != this)
        {
            _activeTrigger.UnregisterConfirmListeners();
        }

        _activeTrigger = this;

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirm);
            confirmButton.onClick.AddListener(OnConfirm);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancel);
            cancelButton.onClick.AddListener(OnCancel);
        }
    }

    private void UnregisterConfirmListeners()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirm);
        }
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancel);
        }

        if (_activeTrigger == this)
        {
            _activeTrigger = null;
        }
    }

    private void OnPreDialogComplete()
    {
        if (confirmUI != null)
        {
            if (confirmTitleText != null)
                confirmTitleText.text = confirmTitle;
            if (confirmDescText != null)
                confirmDescText.text = confirmContent;

            confirmUI.SetActive(true);
        }
    }

    public string GetCharacterName()
    {
        return storyDotConfig != null ? storyDotConfig.characterName : string.Empty;
    }

    private void OnConfirm()
    {
        if (confirmUI != null)
            confirmUI.SetActive(false);

        UnregisterConfirmListeners();

        string characterName = GetCharacterName();
        Debug.Log($"[OnConfirm] 点击的角色头像: {characterName}");
        
        if (!string.IsNullOrEmpty(characterName) && AffectionManager.Instance != null)
        {
            AffectionManager.Instance.AddAffection(characterName, 10);
            Debug.Log($"[OnConfirm] 为 {characterName} 增加了10点好感度");
            
            if (JobListManager.Instance != null)
            {
                JobListManager.Instance.RefreshAll();
                Debug.Log($"[OnConfirm] 刷新打工列表UI");
            }
        }
        
        PlayMainDialog();
    }

    private void OnCancel()
    {
        if (confirmUI != null)
            confirmUI.SetActive(false);

        UnregisterConfirmListeners();

        // 取消 → 重新打开地图
        if (showMapButton != null && !showMapButton.isShow)
        {
            showMapButton.ShowMap();
        }
    }

    private void PlayMainDialog()
    {
        isPlaying = true;

        // 先订阅对话结束事件（此时物体还处于激活状态）
        if (normalEndEvent != null)
        {
            normalEndEvent.DialogEnded -= OnDialogEnded;
            normalEndEvent.DialogEnded += OnDialogEnded;
        }

        // 关闭地图（会导致当前物体 SetActive(false)，触发 OnDisable）
        if (showMapButton != null && showMapButton.isShow)
        {
            showMapButton.ShowMap();
        }

        // 隐藏额外 UI
        ShowDialog.Instance?.HideImmediate();

        // 启动对话
        PlayingTrigger = this;
        BlackoutTransition.Instance.FadeInWithCallback(
            () =>
            {
                var dialog = DialogLocator.Instance.Dialog;
                if (dialog != null && !dialog.activeSelf)
                    dialog.SetActive(true);
                DialogList.Instance.InvokeDialog(_currentStoryDialogIndex, 0);
            },
            fadeInDuration: 0.3f, holdDuration: 0.6f, fadeOutDuration: 0.3f);
        
    }

    private void OnDialogEnded()
    {
        if (!isPlaying) return;
        isPlaying = false;
        PlayingTrigger = null;

        // 取消订阅
        if (normalEndEvent != null)
        {
            normalEndEvent.DialogEnded -= OnDialogEnded;
        }

        // 标记剧情完成
        if (_currentStoryDialogIndex >= 0 && storyDotConfig != null && StoryDotManager.Instance != null)
        {
            StoryDotManager.Instance.MarkCompleted(storyDotConfig.characterName, _currentStoryDialogIndex);
            _currentStoryDialogIndex = -1;
            if (storyDotIndicator != null)
                storyDotIndicator.Refresh();
        }

        // 恢复额外 UI
        ShowDialog.Instance?.Hide2();
    }

    public void RestorePlayingState(int dialogIndex)
    {
        _currentStoryDialogIndex = dialogIndex;
        isPlaying = true;
        PlayingTrigger = this;
        if (normalEndEvent == null)
            normalEndEvent = FindObjectOfType<NormalEndEvent>();
        if (normalEndEvent != null)
        {
            normalEndEvent.DialogEnded -= OnDialogEnded;
            normalEndEvent.DialogEnded += OnDialogEnded;
        }
    }

    private void OnDisable()
    {
        // 注意：不在这里取消订阅 DialogEnded
        // 因为关闭地图会导致本物体 SetActive(false)，但对话仍在播放
        // 订阅的生命周期由 PlayMainDialog/OnDialogEnded 管理

        UnregisterConfirmListeners();
    }

    private void OnDestroy()
    {
        // 物体销毁时才确保清理事件
        if (normalEndEvent != null)
        {
            normalEndEvent.DialogEnded -= OnDialogEnded;
        }
        // 指示器的配置注销由 StoryDotIndicator.OnDestroy 自行处理
    }

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// </summary>
    public static void ResetStaticState()
    {
        PlayingTrigger = null;
        _activeTrigger = null;
        Debug.Log("[DialogTrigger] 静态状态已重置: PlayingTrigger=null, _activeTrigger=null");
    }
}
