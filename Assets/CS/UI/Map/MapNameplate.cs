using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 挂载在名牌上。通过直接检测鼠标位置来判断点击，绕过遮罩对 Graphic 事件的阻挡。
/// </summary>
public class MapNameplate : MonoBehaviour
{
    /// <summary>任意名牌被点击时触发</summary>
    public static event System.Action OnAnyNameplateClicked;

    [Header("名牌详情预制体")]
    public GameObject detailPrefab;

    [Header("名牌信息")]
    public string title = "地点名称";
    [TextArea(2, 5)]
    public string content = "这里是地点描述内容...";

    [Header("地图上对应的位置点（可选，不设置则用名牌自身位置）")]
    public Transform worldTarget;

    [Header("渐入渐出时长（秒）")]
    public float fadeDuration = 0.3f;

    [Tooltip("无操作自动关闭时间（秒）")]
    public float autoHideDelay = 3f;

    [Tooltip("地图缩放到此值以下时自动关闭详情（0=不生效）")]
    public float hideBelowScale = 0.9f;

    [Header("详情面板偏移量")]
    public Vector2 offset = new Vector2(200, 100);

    private GameObject currentDetail;
    private CanvasGroup detailCanvasGroup;
    private bool isShowing = false;
    private bool isHovered = false;
    private Coroutine fadeCoroutine;
    private float hideTimer = 0f;
    private RectTransform myRect;
    private Canvas rootCanvas;
    private static MapNameplate currentShowing;

    private void Awake()
    {
        myRect = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        Debug.Log($"[MapNameplate] {gameObject.name} 初始化, rect={myRect.rect}, canvas={rootCanvas != null}");
    }

    private void Update()
    {
        // 同步名牌位置到地图上的目标点
        if (worldTarget != null)
        {
            transform.position = worldTarget.position;
        }

        Vector2 mousePos = Input.mousePosition;
        bool insideNameplate = IsInsideNameplate(mousePos);

        // 悬停检测
        if (insideNameplate && !isHovered)
        {
            isHovered = true;
            if (SoundsManager.Instance != null)
                SoundsManager.Instance.PlayHoverSfx();
        }
        else if (!insideNameplate && isHovered)
        {
            isHovered = false;
        }

        // 无操作自动关闭
        if (isShowing)
        {
            hideTimer += Time.deltaTime;
            if (hideTimer >= autoHideDelay)
            {
                HideDetail();
            }
        }

        // 地图缩放太小时禁用点击（已打开的详情不受影响，仍可关闭）
        bool scaleBelowThreshold = false;
        if (hideBelowScale > 0f)
        {
            Transform mapTransform = transform.parent;
            if (mapTransform != null && mapTransform.localScale.x < hideBelowScale)
                scaleBelowThreshold = true;
        }

        if (scaleBelowThreshold) return;

        // 鼠标在头像上时，阻断名牌的点击
        if (Input.GetMouseButtonDown(0) && !HoverZoom.IsAnyAvatarHoveredAtAll())
        {
            if (isShowing)
            {
                hideTimer = 0f;
                // 点击名牌本身 → 切换关闭；点击详情外的空白区域 → 也关闭
                if (insideNameplate || !IsInsideDetail(mousePos))
                {
                    HideDetail();
                }
            }
            else
            {
                if (insideNameplate)
                {
                    ShowDetail();
                }
            }
        }
    }

    /// <summary>
    /// 检测鼠标是否在名牌范围内
    /// </summary>
    private bool IsInsideNameplate(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(myRect, screenPos, rootCanvas.worldCamera);
    }

    /// <summary>
    /// 检测鼠标是否在详情面板范围内
    /// </summary>
    private bool IsInsideDetail(Vector2 screenPos)
    {
        if (currentDetail == null) return false;

        RectTransform detailRect = currentDetail.GetComponent<RectTransform>();
        if (detailRect == null) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(detailRect, screenPos, rootCanvas.worldCamera);
    }

    public void ShowDetail()
    {
        if (isShowing) return;

        // 触发名牌点击事件
        OnAnyNameplateClicked?.Invoke();

        // 关闭其他正在显示的详情
        if (currentShowing != null && currentShowing != this)
        {
            currentShowing.HideDetail();
        }

        // 如果当前正在淡出（被中断），立即销毁旧的，避免延迟回调误删新面板
        if (currentDetail != null)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            Destroy(currentDetail);
            currentDetail = null;
            detailCanvasGroup = null;
        }

        isShowing = true;
        hideTimer = 0f;
        currentShowing = this;

        if (SoundsManager.Instance != null)
            SoundsManager.Instance.PlayClickSfx();

        // 实例化到名牌同级
        currentDetail = Instantiate(detailPrefab, transform.parent);
        currentDetail.transform.SetAsLastSibling();

        // 添加高优先级 Canvas 确保弹框在最顶层
        Canvas detailCanvas = currentDetail.GetComponent<Canvas>();
        if (detailCanvas == null)
        {
            detailCanvas = currentDetail.AddComponent<Canvas>();
        }
        detailCanvas.overrideSorting = true;
        detailCanvas.sortingOrder = 100;

        // 设置位置
        RectTransform detailRect = currentDetail.GetComponent<RectTransform>();
        if (detailRect != null)
        {
            detailRect.anchoredPosition = myRect.anchoredPosition + offset;
        }

        // 设置内容
        MapNameplateDetail detail = currentDetail.GetComponent<MapNameplateDetail>();
        if (detail != null)
        {
            detail.Setup(title, content);
        }

        // CanvasGroup 渐入
        detailCanvasGroup = currentDetail.GetComponent<CanvasGroup>();
        if (detailCanvasGroup == null)
        {
            detailCanvasGroup = currentDetail.AddComponent<CanvasGroup>();
        }

        detailCanvasGroup.alpha = 0f;
        StartFade(0f, 1f);
    }

    public void HideDetail()
    {
        if (!isShowing || currentDetail == null) return;

        isShowing = false;

        if (currentShowing == this)
        {
            currentShowing = null;
        }

        var detailToDestroy = currentDetail;
        StartFade(1f, 0f, () =>
        {
            // 只销毁自己，如果期间被 ShowDetail 替换了就不处理
            if (detailToDestroy != null && detailToDestroy == currentDetail)
            {
                Destroy(currentDetail);
                currentDetail = null;
                detailCanvasGroup = null;
            }
        });
    }

    private void StartFade(float from, float to, System.Action onComplete = null)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeCoroutine(from, to, onComplete));
    }

    private IEnumerator FadeCoroutine(float from, float to, System.Action onComplete)
    {
        if (detailCanvasGroup == null) yield break;

        float elapsed = 0f;
        detailCanvasGroup.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            detailCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        detailCanvasGroup.alpha = to;
        onComplete?.Invoke();
    }

    private void OnDestroy()
    {
        if (currentDetail != null)
        {
            Destroy(currentDetail);
        }
    }

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// </summary>
    public static void ResetStaticState()
    {
        currentShowing = null;
        Debug.Log("[MapNameplate] 静态状态已重置: currentShowing=null");
    }
}
