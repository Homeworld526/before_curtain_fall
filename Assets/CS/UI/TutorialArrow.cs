using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 教学引导箭头，指向目标 UI 元素，带上下浮动动画。
/// 支持每帧跟踪目标位置（地图缩放/拖拽时箭头跟随）。
/// </summary>
public class TutorialArrow : MonoBehaviour
{
    [Header("动画设置")]
    public float floatAmplitude = 15f;
    public float floatSpeed = 3f;

    [Header("组件引用")]
    public Image arrowImage;

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Canvas _rootCanvas;

    // 跟踪目标
    private RectTransform _uiTarget;
    private Transform _sceneTarget;
    private ArrowDirection currentDir = ArrowDirection.Down;
    private Vector2 _offset;
    private float _gap = 40f;

    private Vector2 basePosition;
    private bool isAnimating = false;
    private bool _initialized = false;

    public void Initialize()
    {
        if (_initialized) return;

        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
            _rectTransform = gameObject.AddComponent<RectTransform>();

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (arrowImage == null)
            arrowImage = GetComponent<Image>();

        _rootCanvas = GetComponentInParent<Canvas>(true);
        _initialized = true;
    }

    private void Update()
    {
        if (!isAnimating) return;

        // 每帧重新计算目标位置（跟随地图缩放/拖拽/场景物体移动）
        if (_uiTarget != null || _sceneTarget != null)
        {
            RecalcBasePosition();
        }

        // 浮动动画
        float floatOff = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        Vector2 pos = basePosition;
        switch (currentDir)
        {
            case ArrowDirection.Up:
            case ArrowDirection.Down:
                pos.y += floatOff;
                break;
            case ArrowDirection.Left:
            case ArrowDirection.Right:
                pos.x += floatOff;
                break;
        }

        _rectTransform.anchoredPosition = pos;
    }

    /// <summary>
    /// 指向 UI 元素（RectTransform）
    /// </summary>
    public void PointTo(RectTransform target, ArrowDirection dir, Vector2 offset)
    {
        if (target == null) return;
        Initialize();

        gameObject.SetActive(true);
        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>();

        _canvasGroup.alpha = 1f;
        _uiTarget = target;
        _sceneTarget = null;
        currentDir = dir;
        _offset = offset;

        SetArrowRotationByDir(dir);
        RecalcBasePosition();
        _rectTransform.anchoredPosition = basePosition;
        isAnimating = true;
    }

    /// <summary>
    /// 指向场景物体（Transform）
    /// </summary>
    public void PointToScene(Transform target, ArrowDirection dir, Vector2 offset)
    {
        if (target == null) return;
        Initialize();

        gameObject.SetActive(true);
        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>();

        _canvasGroup.alpha = 1f;
        _sceneTarget = target;
        _uiTarget = null;
        currentDir = dir;
        _offset = offset;

        SetArrowRotationByDir(dir);
        RecalcBasePosition();
        _rectTransform.anchoredPosition = basePosition;
        isAnimating = true;
    }

    /// <summary>
    /// 根据当前目标位置重新计算箭头基础位置
    /// </summary>
    private void RecalcBasePosition()
    {
        Vector2 targetPos;
        if (_sceneTarget != null)
            targetPos = GetSceneTargetScreenPos(_sceneTarget);
        else if (_uiTarget != null)
            targetPos = GetUITargetScreenPos(_uiTarget);
        else
            return;

        Vector2 arrowPos = targetPos + _offset;

        switch (currentDir)
        {
            case ArrowDirection.Up:
                arrowPos.y += _gap;
                break;
            case ArrowDirection.Down:
                arrowPos.y -= _gap;
                break;
            case ArrowDirection.Left:
                arrowPos.x -= _gap;
                break;
            case ArrowDirection.Right:
                arrowPos.x += _gap;
                break;
        }

        basePosition = arrowPos;
    }

    /// <summary>
    /// UI 元素 → Canvas 本地坐标
    /// </summary>
    private Vector2 GetUITargetScreenPos(RectTransform target)
    {
        if (_rootCanvas == null)
            return target.anchoredPosition;

        RectTransform canvasRect = _rootCanvas.GetComponent<RectTransform>();
        Vector3 worldPos = target.TransformPoint(target.rect.center);
        return canvasRect.InverseTransformPoint(worldPos);
    }

    /// <summary>
    /// 场景物体 → Canvas 本地坐标（通过摄像机投影）
    /// </summary>
    private Vector2 GetSceneTargetScreenPos(Transform target)
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector2.zero;

        Vector3 screenPos = cam.WorldToScreenPoint(target.position);

        if (_rootCanvas == null)
            return new Vector2(screenPos.x, screenPos.y);

        // 屏幕坐标 → Canvas 本地坐标
        RectTransform canvasRect = _rootCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.rect.size;

        // 根据 Canvas 的 RenderMode 转换
        if (_rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return new Vector2(
                screenPos.x - canvasSize.x * 0.5f,
                screenPos.y - canvasSize.y * 0.5f);
        }
        else
        {
            // ScreenSpace - Camera 或 World Space
            Vector2 viewportPos = cam.ScreenToViewportPoint(screenPos);
            return new Vector2(
                (viewportPos.x - 0.5f) * canvasSize.x,
                (viewportPos.y - 0.5f) * canvasSize.y);
        }
    }

    private void SetArrowRotationByDir(ArrowDirection dir)
    {
        switch (dir)
        {
            case ArrowDirection.Up:    _rectTransform.localRotation = Quaternion.Euler(0, 0, 0); break;
            case ArrowDirection.Down:  _rectTransform.localRotation = Quaternion.Euler(0, 0, 180); break;
            case ArrowDirection.Left:  _rectTransform.localRotation = Quaternion.Euler(0, 0, 90); break;
            case ArrowDirection.Right: _rectTransform.localRotation = Quaternion.Euler(0, 0, -90); break;
        }
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

    public void Hide()
    {
        isAnimating = false;
        _uiTarget = null;
        _sceneTarget = null;
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
