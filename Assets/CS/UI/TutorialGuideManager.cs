using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 强制引导教学管理器。
/// 教学册子关闭后启动，强迫玩家按步骤点击 UI 元素。
/// 每一步：遮罩 + 箭头 + 文字提示 → 玩家操作 → 下一步。
/// 所有渐变动画由此脚本的协程处理（自身始终 active）。
/// </summary>
public class TutorialGuideManager : MonoBehaviour
{
    public static TutorialGuideManager Instance;

    private static bool _guideCompleted = false;

    /// <summary>
    /// 为 true 时完全禁用引导功能，StartGuide() 调用将被忽略。
    /// </summary>
    [Header("暂时禁用引导")]
    public bool IsGuideDisabled = false;

    /// <summary>
    /// 引导正在进行时为 true。游戏其他系统可以检查此标志来跳过实际操作。
    /// </summary>
    public static bool IsGuideActive { get; private set; } = false;

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void EditorReset()
    {
        _guideCompleted = false;
    }
#endif

    [Header("UI 组件引用")]
    public TutorialHighlightMask highlightMask;
    public TutorialArrow arrow;
    public TextMeshProUGUI tipText;
    public CanvasGroup tooltipPanel;

    [Header("引导步骤配置")]
    public TutorialGuideStep[] steps;

    [Header("系统引用")]
    [Tooltip("引导 UI 所在的 Canvas（必须赋值）")]
    public Canvas guideCanvas;
    public ShowMapButton showMapButton;

    [Header("动画设置")]
    public float fadeDuration = 0.3f;

    private int currentIndex = -1;
    private bool isActive = false;
    private bool waitingForAction = false;
    private float autoTimer = 0f;
    private float scrollAccumulator = 0f;
    private bool mapEventReceived = false;
    private bool nameplateEventReceived = false;
    private bool avatarEventReceived = false;
    private bool jobConfirmedReceived = false;
    private RectTransform clickTarget;
    private Transform clickSceneTarget;
    private Canvas rootCanvas;

    private Coroutine fadeArrowCoroutine;
    private Coroutine fadeMaskCoroutine;
    private Coroutine fadeTooltipCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        ShowMapButton.OnMapOpened += OnMapOpened;
        ShowMapButton.OnMapClosed += OnMapClosed;
        MapNameplate.OnAnyNameplateClicked += OnNameplateClicked;
        DialogTrigger.OnAnyAvatarClicked += OnAvatarClicked;
        TutorialGuideJobBridge.OnJobConfirmed += OnJobConfirmed;
    }

    private void OnDisable()
    {
        ShowMapButton.OnMapOpened -= OnMapOpened;
        ShowMapButton.OnMapClosed -= OnMapClosed;
        MapNameplate.OnAnyNameplateClicked -= OnNameplateClicked;
        DialogTrigger.OnAnyAvatarClicked -= OnAvatarClicked;
        TutorialGuideJobBridge.OnJobConfirmed -= OnJobConfirmed;
    }

    private void Update()
    {
        if (!isActive || !waitingForAction) return;
        if (currentIndex < 0 || currentIndex >= steps.Length) return;

        var step = steps[currentIndex];

        switch (step.triggerType)
        {
            case GuideTriggerType.ClickTarget:
                HandleClickTarget();
                break;
            case GuideTriggerType.ScrollWheel:
                HandleScrollWheel();
                break;
            case GuideTriggerType.AutoDelay:
                HandleAutoDelay();
                break;
            case GuideTriggerType.ClickAnywhere:
                HandleClickAnywhere();
                break;
            case GuideTriggerType.MapOpened:
                if (mapEventReceived) CompleteCurrentStep();
                break;
            case GuideTriggerType.MapClosed:
                if (mapEventReceived) CompleteCurrentStep();
                break;
            case GuideTriggerType.NameplateClicked:
                if (nameplateEventReceived) CompleteCurrentStep();
                break;
            case GuideTriggerType.AvatarClicked:
                if (avatarEventReceived) CompleteCurrentStep();
                break;
            case GuideTriggerType.JobConfirmed:
                if (jobConfirmedReceived) CompleteCurrentStep();
                break;
        }
    }

    #region 触发方式处理

    private void HandleClickTarget()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // UI 目标
            if (clickTarget != null)
            {
                Canvas c = rootCanvas ?? GetComponentInParent<Canvas>();
                if (c == null) return;
                if (RectTransformUtility.RectangleContainsScreenPoint(
                    clickTarget, Input.mousePosition, c.worldCamera))
                {
                    CompleteCurrentStep();
                }
            }
            // 场景物体目标
            else if (clickSceneTarget != null)
            {
                Camera cam = Camera.main;
                if (cam == null) return;
                Vector3 screenPos = cam.WorldToScreenPoint(clickSceneTarget.position);
                float dist = Vector2.Distance(Input.mousePosition, screenPos);
                if (dist < 80f) // 80像素范围内算点击
                {
                    CompleteCurrentStep();
                }
            }
        }
    }

    private void HandleScrollWheel()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            scrollAccumulator += Mathf.Abs(scroll);
            if (scrollAccumulator >= 0.1f)
                CompleteCurrentStep();
        }
    }

    private void HandleAutoDelay()
    {
        autoTimer -= Time.deltaTime;
        if (autoTimer <= 0f)
            CompleteCurrentStep();
    }

    private void HandleClickAnywhere()
    {
        if (Input.GetMouseButtonDown(0))
            CompleteCurrentStep();
    }

    #endregion

    #region 事件回调

    private void OnMapOpened() => mapEventReceived = true;
    private void OnMapClosed() => mapEventReceived = true;
    private void OnNameplateClicked() => nameplateEventReceived = true;
    private void OnAvatarClicked() => avatarEventReceived = true;
    private void OnJobConfirmed() => jobConfirmedReceived = true;

    #endregion

    #region 公共接口

    public static bool IsGuideCompleted() => _guideCompleted;

    public static void SetGuideCompleted()
    {
        _guideCompleted = true;
    }

    public void StartGuide()
    {
        if (IsGuideDisabled)
        {
            Debug.Log("[教学引导] 已禁用，跳过");
            return;
        }

        if (_guideCompleted)
        {
            Debug.Log("[教学引导] 已完成过，跳过");
            return;
        }

        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("[教学引导] 没有配置引导步骤");
            _guideCompleted = true;
            return;
        }

        // 激活引导 Canvas（直接引用，不依赖 GetComponentInParent）
        if (guideCanvas == null)
        {
            Debug.LogError("[教学引导] guideCanvas 未赋值！请在 Inspector 中拖入引导 UI 的 Canvas");
            _guideCompleted = true;
            return;
        }

        if (!guideCanvas.gameObject.activeSelf)
        {
            Debug.Log($"[教学引导] 激活 Canvas: {guideCanvas.name}");
            guideCanvas.gameObject.SetActive(true);
        }

        rootCanvas = guideCanvas;

        // 初始化子组件
        if (arrow != null) arrow.Initialize();
        if (highlightMask != null) highlightMask.Initialize();

        // 打印诊断信息
        Debug.Log($"[教学引导] rootCanvas = {rootCanvas.name}");
        Debug.Log($"[教学引导] arrow = {(arrow != null ? "OK" : "NULL")}");
        Debug.Log($"[教学引导] highlightMask = {(highlightMask != null ? "OK" : "NULL")}");
        Debug.Log($"[教学引导] tooltipPanel = {(tooltipPanel != null ? "OK" : "NULL")}");
        Debug.Log($"[教学引导] tipText = {(tipText != null ? "OK" : "NULL")}");

        // 初始隐藏
        if (arrow != null) arrow.SetAlpha(0f);
        if (highlightMask != null) highlightMask.SetAlpha(0f);
        if (tooltipPanel != null) tooltipPanel.alpha = 0f;
        if (tipText != null) tipText.text = "";

        isActive = true;
        currentIndex = -1;

        Debug.Log("[教学引导] 开始强制引导，共 " + steps.Length + " 步");
        AdvanceToNextStep();
    }

    public void ForceEndGuide()
    {
        isActive = false;
        waitingForAction = false;
        _guideCompleted = true;
        IsGuideActive = false;

        if (arrow != null) arrow.Hide();
        if (highlightMask != null) highlightMask.HideMask();
        HideTooltip();

        Debug.Log("[教学引导] 强制结束");
    }

    public static void ResetForNewGame()
    {
        _guideCompleted = false;
    }

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// </summary>
    public static void ResetStaticState()
    {
        IsGuideActive = false;
        Debug.Log("[TutorialGuideManager] 静态状态已重置: IsGuideActive=false");
    }

    #endregion

    #region 步骤流程

    private void AdvanceToNextStep()
    {
        currentIndex++;

        if (currentIndex >= steps.Length)
        {
            OnGuideComplete();
            return;
        }

        var step = steps[currentIndex];
        Debug.Log($"[教学引导] 进入步骤 {currentIndex}: {step.stepName}");

        // 重置
        mapEventReceived = false;
        nameplateEventReceived = false;
        avatarEventReceived = false;
        jobConfirmedReceived = false;
        scrollAccumulator = 0f;
        autoTimer = step.autoDelay;
        waitingForAction = false;
        clickTarget = step.target;
        clickSceneTarget = step.sceneTarget;

        // 设置是否屏蔽游戏点击（每步独立控制）
        IsGuideActive = step.blockGameInput;
        // 遮罩的黑色区域拦截点击，洞口不拦截
        if (highlightMask != null)
            highlightMask.SetBlockRaycast(step.blockGameInput);
        Debug.Log($"[教学引导] 步骤 {currentIndex} blockGameInput={step.blockGameInput}, IsGuideActive={IsGuideActive}");

        // 执行步骤开始动作
        ExecuteAction(step.actionOnStart);

        // 更新 UI（立即设置，然后渐入）
        ShowArrowImmediate(step);
        ShowHighlightImmediate(step);
        ShowTooltipImmediate(step);

        // 渐入动画（根据步骤配置决定是否显示箭头）
        if (step.showArrow)
            StartFadeArrow(0f, 1f, fadeDuration);
        StartFadeMask(0f, 1f, fadeDuration);
        StartFadeTooltip(0f, 1f, fadeDuration);

        // 延迟后开始等待玩家操作
        StartCoroutine(StartWaitingAfterDelay(fadeDuration + 0.1f));
    }

    private IEnumerator StartWaitingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        waitingForAction = true;
    }

    private void CompleteCurrentStep()
    {
        if (!waitingForAction) return;
        waitingForAction = false;

        var step = steps[currentIndex];
        Debug.Log($"[教学引导] 完成步骤 {currentIndex}: {step.stepName}");

        ExecuteAction(step.actionOnComplete);

        StartCoroutine(NextStepAfterDelay(fadeDuration + 0.2f));
    }

    private IEnumerator NextStepAfterDelay(float delay)
    {
        // 渐出当前 UI
        StartFadeArrow(1f, 0f, fadeDuration);
        StartFadeMask(1f, 0f, fadeDuration);
        StartFadeTooltip(1f, 0f, fadeDuration);

        yield return new WaitForSeconds(delay);

        // 隐藏物体
        if (arrow != null) arrow.Hide();
        if (highlightMask != null) highlightMask.HideMask();

        AdvanceToNextStep();
    }

    private void OnGuideComplete()
    {
        _guideCompleted = true;
        isActive = false;
        IsGuideActive = false;

        // 渐出然后隐藏
        StartFadeArrow(1f, 0f, fadeDuration);
        StartFadeMask(1f, 0f, fadeDuration);
        StartFadeTooltip(1f, 0f, fadeDuration);

        StartCoroutine(HideAfterDelay(fadeDuration));
        Debug.Log("[教学引导] 引导完成！");
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (arrow != null) arrow.Hide();
        if (highlightMask != null) highlightMask.HideMask();
    }

    #endregion

    #region UI 更新（立即设置 + 渐变动画）

    private void ShowArrowImmediate(TutorialGuideStep step)
    {
        if (arrow == null) return;

        // 不需要箭头则直接隐藏
        if (!step.showArrow)
        {
            arrow.Hide();
            return;
        }

        // 优先用 UI 目标，没有则用场景物体
        if (step.target != null)
        {
            arrow.PointTo(step.target, step.arrowDir, step.arrowOffset);
            arrow.SetAlpha(0f);
        }
        else if (step.sceneTarget != null)
        {
            arrow.PointToScene(step.sceneTarget, step.arrowDir, step.arrowOffset);
            arrow.SetAlpha(0f);
        }
        else
        {
            arrow.Hide();
        }
    }

    private void ShowHighlightImmediate(TutorialGuideStep step)
    {
        if (highlightMask == null) return;

        // 高亮遮罩只支持 UI 目标（场景物体没有 RectTransform）
        if (step.useHighlight && step.target != null)
        {
            highlightMask.ShowMask(step.target, step.highlightPadding);
            highlightMask.SetAlpha(0f);
        }
        else
        {
            highlightMask.HideMask();
        }
    }

    private void ShowTooltipImmediate(TutorialGuideStep step)
    {
        if (tipText != null)
            tipText.text = step.tipMessage;
        if (tooltipPanel != null)
            tooltipPanel.alpha = 0f;
    }

    // 箭头渐变（由本脚本协程驱动，自身始终 active）
    private void StartFadeArrow(float from, float to, float duration)
    {
        if (arrow == null) return;
        if (fadeArrowCoroutine != null) StopCoroutine(fadeArrowCoroutine);
        fadeArrowCoroutine = StartCoroutine(FadeCoroutine(from, to, duration, arrow.SetAlpha));
    }

    // 遮罩渐变
    private void StartFadeMask(float from, float to, float duration)
    {
        if (highlightMask == null) return;
        if (fadeMaskCoroutine != null) StopCoroutine(fadeMaskCoroutine);
        fadeMaskCoroutine = StartCoroutine(FadeCoroutine(from, to, duration, highlightMask.SetAlpha));
    }

    // 提示面板渐变
    private void StartFadeTooltip(float from, float to, float duration)
    {
        if (tooltipPanel == null) return;
        if (fadeTooltipCoroutine != null) StopCoroutine(fadeTooltipCoroutine);
        fadeTooltipCoroutine = StartCoroutine(FadeTooltip(from, to, duration));
    }

    private void HideTooltip()
    {
        StartFadeTooltip(tooltipPanel != null ? tooltipPanel.alpha : 0f, 0f, fadeDuration);
    }

    private IEnumerator FadeCoroutine(float from, float to, float duration, System.Action<float> setter)
    {
        float t = 0f;
        setter(from);
        while (t < duration)
        {
            t += Time.deltaTime;
            setter(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        setter(to);
    }

    private IEnumerator FadeTooltip(float from, float to, float duration)
    {
        if (tooltipPanel == null) yield break;

        float t = 0f;
        tooltipPanel.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            tooltipPanel.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        tooltipPanel.alpha = to;
    }

    #endregion

    #region 动作执行

    private void ExecuteAction(GuideAction action)
    {
        switch (action)
        {
            case GuideAction.OpenMap:
                if (showMapButton != null && !showMapButton.isShow)
                    showMapButton.ShowMap();
                break;
            case GuideAction.CloseMap:
                if (showMapButton != null && showMapButton.isShow)
                    showMapButton.ShowMap();
                break;
            case GuideAction.OpenJobPanel:
                if (TutorialGuideJobBridge.Instance != null)
                    TutorialGuideJobBridge.Instance.OpenJobPanel();
                break;
            case GuideAction.CloseJobPanel:
                if (TutorialGuideJobBridge.Instance != null)
                    TutorialGuideJobBridge.Instance.CloseJobPanel();
                break;
            case GuideAction.ZoomMapToMin:
                if (showMapButton != null) showMapButton.ZoomToMin();
                break;
            case GuideAction.ZoomMapToMax:
                if (showMapButton != null) showMapButton.ZoomToMax();
                break;
            case GuideAction.None:
            default:
                break;
        }
    }

    #endregion

    #region 测试用

    [ContextMenu("重置引导记录 (测试用)")]
    private void ResetGuideTest()
    {
        _guideCompleted = false;
        Debug.Log("[教学引导] 已重置引导记录");
    }

    [ContextMenu("立即开始引导 (测试用)")]
    private void StartGuideTest()
    {
        _guideCompleted = false;
        StartGuide();
    }

    private void OnApplicationQuit()
    {
        _guideCompleted = false;
    }

    #endregion
}
