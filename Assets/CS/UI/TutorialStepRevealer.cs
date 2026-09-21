using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialStepRevealer : MonoBehaviour
{
    [Header("把教程的【每一行Object】按顺序拖进这里")]
    public GameObject[] rows;

    // 内存标记：逐行播放是否已完成
    private static bool _animCompleted = false;

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void EditorReset()
    {
        _animCompleted = false;
    }
#endif

    // 如果由剧情结束时自动弹出，调用方请在启用面板前把这个 flag 设为 true
    public bool openedByAuto = false;

    // 如果是玩家通过 UI 按钮手动打开（例如从教学合集里点开），请把这个 flag 设为 true
    // 手动打开时始终直接显示完整教学（不走逐行逻辑）
    public bool openedByButton = false;

    // 静态标记：按钮点击时设置，OnEnable 读取后重置
    public static bool nextOpenByButton = false;

    [Header("如果玩家不点，自动播放下一行的延迟（秒），设为 <=0 则禁用自动播放")]
    public float autoRevealDelay = 2f;

    [Header("自动滚动")]
    [Tooltip("教程内容的ScrollRect，新行出现时自动滚到底部")]
    public ScrollRect scrollRect;
    [Tooltip("教程开始后延迟多少秒再开始滚动")]
    public float scrollStartDelay = 10f;

    [Header("渐入/渐出配置")]
    [Tooltip("单行渐入/渐出的时长（秒）")]
    public float fadeDuration = 0.25f;

    [Tooltip("在逐行播放完毕后，是否对整个面板执行渐出并自动关闭面板")]
    public bool fadeOutOnFinish = false;

    private int currentIndex = 0;
    private bool isAnimating = false;
    private float timer = 0f;
    private CanvasGroup panelCanvasGroup;

    private void OnEnable()
    {
        // 检查静态标记：按钮点击时设置的
        if (nextOpenByButton)
        {
            openedByButton = true;
            nextOpenByButton = false;
        }

        // 已播放过且不是手动打开，直接关闭
        if (!openedByAuto && !openedByButton && _animCompleted)
        {
            Debug.Log("[教学StepRevealer] 已看过教学，自动关闭");
            gameObject.SetActive(false);
            return;
        }

        // 重置滚动位置
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;

        // 确保面板也有 CanvasGroup，便于整个面板渐出
        panelCanvasGroup = GetOrAddCanvasGroup(this.gameObject);
        // 判断是否进入逐行播放状态：
        // - 如果是剧情自动弹出 (openedByAuto == true) -> 一定逐行播放
        // - 如果是玩家通过按钮手动打开 (openedByButton == true) -> 直接全部显示
        // - 否则按内存标记决定（未看过则逐行播放，已看过则直接显示）
        if (openedByAuto)
        {
            isAnimating = true;
            currentIndex = 0;
            timer = autoRevealDelay;

            // 先把所有行关掉
            foreach (var row in rows)
            {
                if (row == null) continue;
                row.SetActive(false);
            }

            // 只激活第一行并渐入
            if (rows.Length > 0 && rows[0] != null)
            {
                rows[0].SetActive(true);
                var cg0 = GetOrAddCanvasGroup(rows[0]);
                cg0.alpha = 0f;
                StartCoroutine(FadeCanvasGroup(cg0, 0f, 1f, fadeDuration, () =>
                {
                    cg0.interactable = true;
                    cg0.blocksRaycasts = true;
                    StartSmoothScroll();
                }));
            }
        }
        else if (openedByButton)
        {
            isAnimating = false;
            // 按钮打开：直接显示全部行（即时）
            StartSmoothScroll();
            foreach (var row in rows)
            {
                if (row == null) continue;
                row.SetActive(true);
                var cg = GetOrAddCanvasGroup(row);
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }
        else
        {
            if (!_animCompleted)
            {
                isAnimating = true;
                currentIndex = 0;
                timer = autoRevealDelay;

                // 先把所有行关掉
                foreach (var row in rows)
                {
                    if (row == null) continue;
                    row.SetActive(false);
                }

                // 只激活第一行并渐入
                if (rows.Length > 0 && rows[0] != null)
                {
                    rows[0].SetActive(true);
                    var cg0 = GetOrAddCanvasGroup(rows[0]);
                    cg0.alpha = 0f;
                    StartCoroutine(FadeCanvasGroup(cg0, 0f, 1f, fadeDuration, () =>
                    {
                        cg0.interactable = true;
                        cg0.blocksRaycasts = true;
                        StartSmoothScroll();
                    }));
                }
            }
            else
            {
                isAnimating = false;
                // 直接显示全部行（即时）
                StartSmoothScroll();
                foreach (var row in rows)
                {
                    if (row == null) continue;
                    row.SetActive(true);
                    var cg = GetOrAddCanvasGroup(row);
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }
        }

        // 清理一次性标志，避免下次重复使用时产生意外
        openedByAuto = false;
        openedByButton = false;
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(1))
        {
            gameObject.SetActive(false);
            return;
        }

        // 如果不在逐行播放状态，就不执行任何操作
        if (!isAnimating) return;

        // 点击推进优先
        if (Input.GetMouseButtonDown(0))
        {
            AdvanceNext();
            return;
        }

        // 自动推进（如果启用）
        if (autoRevealDelay > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                AdvanceNext();
            }
        }
    }

    private void AdvanceNext()
    {
        currentIndex++;

        if (currentIndex < rows.Length)
        {
            if (rows[currentIndex] != null)
            {
                // 先激活，让布局计算高度
                rows[currentIndex].SetActive(true);
                var cg = GetOrAddCanvasGroup(rows[currentIndex]);
                cg.alpha = 0f;
                StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, fadeDuration, () =>
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }));
            }

            timer = autoRevealDelay;
        }
        else
        {
            isAnimating = false;
            _animCompleted = true;

            // 完成后确保所有行都可交互
            foreach (var row in rows)
            {
                if (row == null) continue;
                var cg = GetOrAddCanvasGroup(row);
                cg.interactable = true;
                cg.blocksRaycasts = true;
                cg.alpha = 1f;
            }

            if (fadeOutOnFinish)
            {
                if (panelCanvasGroup == null) panelCanvasGroup = GetOrAddCanvasGroup(this.gameObject);
                StartCoroutine(FadeCanvasGroup(panelCanvasGroup, 1f, 0f, fadeDuration, () =>
                {
                    this.gameObject.SetActive(false);
                }));
            }
        }
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, System.Action onComplete = null)
    {
        if (cg == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float t = 0f;
        cg.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, duration <= 0f ? 1f : t / duration);
            yield return null;
        }

        cg.alpha = to;
        onComplete?.Invoke();
    }

    // 尝试判断是否由玩家点击 UI 按钮触发打开：
    // - 如果 EventSystem.current.currentSelectedGameObject 非空（常见于 Button 的 OnClick 回调）
    // - 或者本帧检测到鼠标/触摸输入
    private bool DetectOpenedByButton()
    {
        if (EventSystem.current != null)
        {
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                return true;
            }

            // 如果指针当前在 UI 上，也很可能是手动操作
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }
        }

        // 检查输入：鼠标点击或触摸
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) return true;
        if (Input.touchCount > 0) return true;

        return false;
    }

    private Coroutine scrollCoroutine;

    private void StartSmoothScroll()
    {
        if (scrollRect == null) return;
        if (scrollCoroutine != null) StopCoroutine(scrollCoroutine);
        scrollCoroutine = StartCoroutine(SmoothScrollCoroutine());
    }

    private System.Collections.IEnumerator SmoothScrollCoroutine()
    {
        yield return new WaitForSeconds(scrollStartDelay);
        Canvas.ForceUpdateCanvases();

        float totalDuration = rows.Length; // 总滚动秒数 = 行数
        float start = 1f;
        float end = 0f;
        float t = 0f;

        while (t < totalDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / totalDuration);
            // easeOut：开始快，后面越来越慢，像自然减速
            float eased = 1f - Mathf.Pow(1f - progress, 2f);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, end, eased);
            yield return null;
        }
        scrollRect.verticalNormalizedPosition = end;
    }

    // 测试用按钮：在 Unity 面板右键该脚本可以重置记录
    [ContextMenu("重置逐行播放记录 (用于测试)")]
    private void ResetAnim()
    {
        _animCompleted = false;
        Debug.Log("已重置逐行动画记录，下次打开将再次逐行播放。");
    }

    private void OnApplicationQuit()
    {
        _animCompleted = false;
    }

    public static void ResetForNewGame()
    {
        _animCompleted = false;
        nextOpenByButton = false;
    }

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// </summary>
    public static void ResetStaticState()
    {
        ResetForNewGame();
        Debug.Log("[TutorialStepRevealer] 静态状态已重置");
    }
}
