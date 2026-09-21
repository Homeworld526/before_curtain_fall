using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 鼠标靠近时放大，离开时恢复。绕过EventSystem，直接检测鼠标位置。
/// </summary>
public class HoverZoom : MonoBehaviour
{
    public float hoverScale = 1.2f;
    public float duration = 0.25f;
    public Ease ease = Ease.OutBack;

    [Header("悬停替换图片（可选，不设置则只放大）")]
    public Sprite hoverSprite;

    [Header("点击事件")]
    public UnityEngine.Events.UnityEvent onClick;

    public bool IsHovered => isHovered;
    public float HoverScaleValue => hoverScale;

    /// <summary>
    /// 当前是否有头像被鼠标悬停（供 MapNameplate 阻断穿透用，只针对头像）
    /// </summary>
    public static bool IsAnyAvatarHovered { get; private set; }

    private Vector3 originalScale;
    private Tweener currentTween;
    private RectTransform myRect;
    private Canvas rootCanvas;
    private Image image;
    private Sprite originalSprite;
    private bool isHovered;

    private bool isAvatar;

    void Start()
    {
        originalScale = transform.localScale;
        myRect = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        image = GetComponent<Image>();
        if (image != null)
            originalSprite = image.sprite;
        isAvatar = ShowMapButton.instance != null && ShowMapButton.instance.IsAvatar(gameObject);
    }

    void Update()
    {
        if (myRect == null || rootCanvas == null) return;

        bool inside = RectTransformUtility.RectangleContainsScreenPoint(
            myRect, Input.mousePosition, rootCanvas.worldCamera);

        if (inside && !isHovered)
        {
            isHovered = true;
            if (isAvatar) IsAnyAvatarHovered = true;
            if (!isAvatar)
            {
                currentTween?.Kill();
                currentTween = transform.DOScale(originalScale * hoverScale, duration).SetEase(ease);
            }
            if (hoverSprite != null && image != null)
                image.sprite = hoverSprite;
            if (SoundsManager.Instance != null)
                SoundsManager.Instance.PlayHoverSfx();
        }
        else if (!inside && isHovered)
        {
            isHovered = false;
            if (isAvatar) IsAnyAvatarHovered = FindAnyAvatarHovered();
            if (!isAvatar)
            {
                currentTween?.Kill();
                currentTween = transform.DOScale(originalScale, duration).SetEase(ease);
            }
            if (hoverSprite != null && image != null)
                image.sprite = originalSprite;
        }

        // 点击检测（只针对头像等非 Button 元素，Button 按钮由 UISoundPlayer 处理）
        if (isAvatar && inside && Input.GetMouseButtonDown(0))
        {
            Debug.Log($"[HoverZoom] 头像点击，onClick 绑定事件数量: {onClick.GetPersistentEventCount()}");
            if (SoundsManager.Instance != null)
                SoundsManager.Instance.PlayClickSfx();
            onClick?.Invoke();
        }
    }

    private static bool FindAnyAvatarHovered()
    {
        HoverZoom[] all = FindObjectsOfType<HoverZoom>();
        foreach (var hz in all)
        {
            if (hz.isAvatar && hz.isHovered) return true;
        }
        return false;
    }

    /// <summary>
    /// 是否有头像被鼠标悬停（供名牌点击阻断用）
    /// </summary>
    public static bool IsAnyAvatarHoveredAtAll()
    {
        HoverZoom[] all = FindObjectsOfType<HoverZoom>();
        foreach (var hz in all)
        {
            if (hz.isAvatar && hz.isHovered) return true;
        }
        return false;
    }

    /// <summary>
    /// 是否有其他 HoverZoom 对象被鼠标悬停（排除 self）
    /// </summary>
    public static bool IsOtherHovered(GameObject self)
    {
        HoverZoom[] all = FindObjectsOfType<HoverZoom>();
        foreach (var hz in all)
        {
            if (hz.isHovered && hz.gameObject != self) return true;
        }
        return false;
    }

    void OnDestroy()
    {
        currentTween?.Kill();
    }

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// </summary>
    public static void ResetStaticState()
    {
        IsAnyAvatarHovered = false;
        Debug.Log("[HoverZoom] 静态状态已重置: IsAnyAvatarHovered=false");
    }
}
