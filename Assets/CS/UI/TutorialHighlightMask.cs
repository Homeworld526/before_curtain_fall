using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 高亮遮罩：全屏半透明黑 + 在目标处挖洞。
/// 4 个 Image 用 anchor(0.5,0.5) 定位，和 Canvas 本地坐标系对齐。
/// </summary>
public class TutorialHighlightMask : MonoBehaviour
{
    [Header("遮罩颜色")]
    public Color maskColor = new Color(0f, 0f, 0f, 0.65f);

    private Image imgTop, imgBottom, imgLeft, imgRight;
    private CanvasGroup _canvasGroup;
    private Canvas _rootCanvas;
    private RectTransform _canvasRect;
    private bool _initialized = false;
    private RectTransform _target;
    private Vector2 _padding;

    public void Initialize()
    {
        if (_initialized) return;

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // ★ 禁用根物体自身的 Image（避免白色默认图盖住画面）
        var rootImg = GetComponent<Image>();
        if (rootImg != null) rootImg.enabled = false;

        _rootCanvas = GetComponentInParent<Canvas>(true);
        if (_rootCanvas != null)
            _canvasRect = _rootCanvas.GetComponent<RectTransform>();

        imgTop = CreatePiece("Top");
        imgBottom = CreatePiece("Bottom");
        imgLeft = CreatePiece("Left");
        imgRight = CreatePiece("Right");

        _initialized = true;
    }

    private Image CreatePiece(string name)
    {
        var go = new GameObject("Mask_" + name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);

        var img = go.GetComponent<Image>();
        img.color = maskColor;
        img.raycastTarget = false; // ★ 不拦截点击，让引导系统自己检测
        img.sprite = null;
        Debug.Log($"[遮罩] 创建 {name}, color={maskColor}");

        // anchor 设为 (0.5, 0.5)，和 Canvas 本地坐标系 (0,0)在中心 对齐
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        return img;
    }

    public void ShowMask(RectTransform target, Vector2 padding)
    {
        if (target == null) return;
        Initialize();
        gameObject.SetActive(true);

        _target = target;
        _padding = padding;

        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas != null)
            _canvasRect = _rootCanvas.GetComponent<RectTransform>();

        _canvasGroup.alpha = 1f;
        UpdateMaskRects();
    }

    private void Update()
    {
        if (_target != null && _canvasGroup != null && _canvasGroup.alpha > 0f)
            UpdateMaskRects();
    }

    private void UpdateMaskRects()
    {
        if (_target == null || _canvasRect == null) return;

        // 目标的世界坐标四个角 → Canvas 本地坐标
        Vector3[] corners = new Vector3[4];
        _target.GetWorldCorners(corners);
        Vector2 bl = _canvasRect.InverseTransformPoint(corners[0]);
        Vector2 tr = _canvasRect.InverseTransformPoint(corners[2]);

        float L = bl.x - _padding.x;
        float R = tr.x + _padding.x;
        float B = bl.y - _padding.y;
        float T = tr.y + _padding.y;

        // Canvas 半尺寸
        Vector2 hs = _canvasRect.rect.size * 0.5f;

        // 上：全宽，从 T 到 +hs.y
        Place(imgTop,    0,       (T + hs.y) * 0.5f,       hs.x * 2,    hs.y - T);
        // 下：全宽，从 -hs.y 到 B
        Place(imgBottom, 0,       (-hs.y + B) * 0.5f,      hs.x * 2,    B + hs.y);
        // 左：从 L 到 -hs.x，高度 = T - B
        Place(imgLeft,   (-hs.x + L) * 0.5f, (T + B) * 0.5f, L + hs.x,   T - B);
        // 右：从 R 到 +hs.x，高度 = T - B
        Place(imgRight,  (R + hs.x) * 0.5f,  (T + B) * 0.5f, hs.x - R,   T - B);
    }

    /// <summary>
    /// 设置 Image 的位置和大小
    /// </summary>
    private void Place(Image img, float cx, float cy, float w, float h)
    {
        if (img == null) return;
        var rt = img.rectTransform;
        rt.anchoredPosition = new Vector2(cx, cy);
        rt.sizeDelta = new Vector2(Mathf.Max(0, w), Mathf.Max(0, h));
    }

    public void SetAlpha(float alpha)
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = alpha;
    }

    public float GetAlpha()
    {
        return _canvasGroup != null ? _canvasGroup.alpha : 0f;
    }

    public void HideMask()
    {
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 设置遮罩是否拦截点击。
    /// true = 黑色区域拦截点击（场景物体点不到），洞口不拦截。
    /// false = 完全不拦截（穿透点击）。
    /// </summary>
    public void SetBlockRaycast(bool block)
    {
        if (imgTop != null)    imgTop.raycastTarget = block;
        if (imgBottom != null) imgBottom.raycastTarget = block;
        if (imgLeft != null)   imgLeft.raycastTarget = block;
        if (imgRight != null)  imgRight.raycastTarget = block;
    }
}
