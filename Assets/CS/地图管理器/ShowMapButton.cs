using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShowMapButton : MonoBehaviour
{
    public Button button;
    public GameObject map;
    public GameObject blurMask;
    public GameObject BianKuang;
    public float fadeTime = 0.2f;
    public static ShowMapButton instance;

    /// <summary>地图打开时触发</summary>
    public static event System.Action OnMapOpened;
    /// <summary>地图关闭时触发</summary>
    public static event System.Action OnMapClosed;

    public CanvasGroup group;
    
    [Header("缩放设置")]
    public float minScale = 0.5f;
    public float maxScale = 2.0f;
    public float scaleSpeed = 0.25f;
    public Transform mapParent;

    [Header("拖拽设置")]
    public RectTransform viewport; // 蒙版/可视区域，拖拽只在此范围内生效
    public bool limitDrag = true;
    public float dragSpeed = 2f;

    [Header("名牌显示设置")]
    public GameObject[] nameTags;   // 所有名牌
    public float showNameScale = 1.4f; // 放大到多少显示名牌
    public float fadeSpeed = 5f;       // alpha渐入渐出速度

    [Header("头像设置")]
    public GameObject[] avatars;       // 地图上的角色头像

    public bool isShow { get; private set; }
    // 地图正在占用输入时为true，让2D交互脚本跳过检测
    public static bool IsMapConsumingInput { get; private set; }
    private Vector3 originalPos;
    private Vector3 originalScale;
    private RectTransform mapRect;
    private RectTransform parentRect;
    private Vector3 targetScale;
    private bool isDragging;
    private Vector3[] avatarOriginalScales;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// 注意：不清空静态事件，因为订阅者是实例对象，会在 OnDestroy 中自动取消订阅
    /// </summary>
    public static void ResetStaticState()
    {
        IsMapConsumingInput = false;
        // 不清空静态事件，保留订阅关系
        // OnMapOpened = null;
        // OnMapClosed = null;
        Debug.Log("[ShowMapButton] 静态状态已重置（保留事件订阅）");
    }
    
    void Start()
    {
        map.SetActive(false);
        BianKuang.SetActive(false);
        blurMask.SetActive(false);
        group.interactable = false;
        group.blocksRaycasts = false;
        button.onClick.AddListener(ShowMap);

        mapRect = map.GetComponent<RectTransform>();

        // blurMask不阻挡鼠标事件
        Image blurImg = blurMask.GetComponent<Image>();
        if (blurImg != null)
            blurImg.raycastTarget = false;

        // 确保地图Canvas有GraphicRaycaster，否则IsPointerOverGameObject检测不到
        Canvas mapCanvas = map.transform.parent.GetComponent<Canvas>();
        if (mapCanvas != null && mapCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            mapCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (mapParent != null)
            parentRect = mapParent.GetComponent<RectTransform>();
        else
            parentRect = map.transform.parent.GetComponent<RectTransform>();

        originalPos = mapRect.anchoredPosition;
        originalScale = mapRect.localScale;
        targetScale = mapRect.localScale;

        // 存储头像初始缩放
        if (avatars != null && avatars.Length > 0)
        {
            avatarOriginalScales = new Vector3[avatars.Length];
            for (int i = 0; i < avatars.Length; i++)
            {
                if (avatars[i] != null)
                    avatarOriginalScales[i] = avatars[i].transform.localScale;
            }
        }

        // 初始隐藏名牌(alpha=0)，用 CanvasGroup 控制整个物体及子物体
        foreach (var tag in nameTags)
        {
            if (tag != null)
            {
                CanvasGroup cg = tag.GetComponent<CanvasGroup>();
                if (cg == null) cg = tag.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
        }

        // 地图默认 inactive，子物体的 StoryDotIndicator.OnEnable 不会执行，
        // 需要手动注册 config 让按钮感叹号在首次打开地图前就能正确显示。
        InitIndicators();
    }

    void Update()
    {
        if (isShow)
        {
            HandleZoom();
            HandleDrag();
            UpdateNameTags();
            UpdateAvatars();

            // 鼠标在蒙版区域内时，阻塞2D交互
            RectTransform checkRect = viewport != null ? viewport : mapRect;
            IsMapConsumingInput = checkRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(checkRect, Input.mousePosition, null);
        }
        else
        {
            IsMapConsumingInput = false;
        }

        if (isShow && Input.GetKeyUp(KeyCode.Escape))
        {
            ShowMap();
        }
    }

    public void ShowMap()
    {
        if (ShowDialog.Instance.isDialogPlaying)
        {
            return;
        }
        isShow = !isShow;


    if (isShow)
    {
        blurMask.SetActive(true);
        BianKuang.SetActive(true);
        map.SetActive(true);
        group.interactable = true;
        group.blocksRaycasts = true;
        OnMapOpened?.Invoke();

        // 地图打开后刷新所有感叹号（首次打开时 OnEnable 时序可能不对）
        RefreshAllIndicators();
    }

    else
    {
        // 关闭地图前清除所有名牌详情
        if (MapNameplateManager.Instance != null)
            MapNameplateManager.Instance.HideAll();

        blurMask.SetActive(false);
        BianKuang.SetActive(false);
        map.SetActive(false);
        group.interactable = false;
        group.blocksRaycasts = false;
        OnMapClosed?.Invoke();
    }
}

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            targetScale += new Vector3(scroll * scaleSpeed, scroll * scaleSpeed, 0);

            targetScale.x = Mathf.Clamp(targetScale.x, minScale, maxScale);
            targetScale.y = Mathf.Clamp(targetScale.y, minScale, maxScale);
            targetScale.z = 1;
        }

        mapRect.localScale = Vector3.Lerp(mapRect.localScale, targetScale, Time.deltaTime * 10f);

        if (limitDrag)
            mapRect.anchoredPosition = ClampMapPosition(mapRect.anchoredPosition);
    }

    void HandleDrag()
    {
        // 鼠标在蒙版范围内按下时才开始拖拽
        if (Input.GetMouseButtonDown(0))
        {
            RectTransform checkRect = viewport != null ? viewport : mapRect;
            if (checkRect != null && RectTransformUtility.RectangleContainsScreenPoint(
                checkRect, Input.mousePosition, null))
            {
                isDragging = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            float deltaX = Input.GetAxis("Mouse X") * dragSpeed * mapRect.localScale.x;
            float deltaY = Input.GetAxis("Mouse Y") * dragSpeed * mapRect.localScale.y;

            Vector2 deltaPos = new Vector2(deltaX, deltaY);
            Vector2 newPos = mapRect.anchoredPosition + deltaPos;

            if (limitDrag)
                newPos = ClampMapPosition(newPos);

            mapRect.anchoredPosition = newPos;
        }
    }

    // 根据缩放控制名牌渐入渐出（通过 CanvasGroup 影响所有子物体）
    void UpdateNameTags()
    {
        if (!isShow) return;

        float scale = mapRect.localScale.x;
        float targetAlpha = scale >= showNameScale ? 1f : 0f;

        foreach (var tag in nameTags)
        {
            if (tag != null)
            {
                CanvasGroup cg = tag.GetComponent<CanvasGroup>();
                if (cg == null) cg = tag.AddComponent<CanvasGroup>();
                cg.alpha = Mathf.Lerp(cg.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            }
        }
    }

    // 头像缩放补偿，保持大小不变
    void UpdateAvatars()
    {
        if (avatars == null) return;

        Vector3 mapScale = mapRect.localScale;
        for (int i = 0; i < avatars.Length; i++)
        {
            if (avatars[i] != null && avatarOriginalScales != null && i < avatarOriginalScales.Length)
            {
                Vector3 baseScale = avatarOriginalScales[i];
                HoverZoom hover = avatars[i].GetComponent<HoverZoom>();
                if (hover != null && hover.IsHovered)
                    baseScale *= hover.HoverScaleValue;

                avatars[i].transform.localScale = new Vector3(
                    baseScale.x / mapScale.x,
                    baseScale.y / mapScale.y,
                    baseScale.z / mapScale.z
                );
            }
        }
    }

    private Vector2 ClampMapPosition(Vector2 position)
    {
        if (mapRect == null || parentRect == null) return position;

        Vector2 mapSize = new Vector2(
            mapRect.rect.width * mapRect.localScale.x,
            mapRect.rect.height * mapRect.localScale.y
        );

        Vector2 parentSize = parentRect.rect.size;

        float xBound = Mathf.Max(0, (mapSize.x - parentSize.x) / 2f);
        float yBound = Mathf.Max(0, (mapSize.y - parentSize.y) / 2f);

        float clampedX = Mathf.Clamp(position.x, -xBound, xBound);
        float clampedY = Mathf.Clamp(position.y, -yBound, yBound);

        return new Vector2(clampedX, clampedY);
    }

    public bool IsAvatar(GameObject obj)
    {
        if (avatars == null) return false;
        for (int i = 0; i < avatars.Length; i++)
        {
            if (avatars[i] == obj) return true;
        }
        return false;
    }

    /// <summary>
    /// 立即把地图缩到指定倍率（0.9 = 略小于原始大小）
    /// </summary>
    /// <summary>
    /// 初始化感叹号状态（不打开地图）。
    /// 地图 inactive 时也能工作：手动注册 config 并设置 alpha。
    /// </summary>
    public void InitIndicators()
    {
        if (map == null) return;

        var indicators = map.GetComponentsInChildren<StoryDotIndicator>(true);

        // 批量注册 config，不触发事件（避免 N 次 NotifyStateChanged）
        if (StoryDotManager.Instance != null)
            StoryDotManager.Instance.BatchRegisterConfigs(indicators);

        // 直接读取状态设置 alpha
        foreach (var ind in indicators)
        {
            if (ind.config == null || ind.exclamationMark == null) continue;

            bool available = StoryDotManager.Instance != null &&
                             StoryDotManager.Instance.IsStoryAvailable(ind.config);
            var cg = ind.exclamationMark.GetComponent<CanvasGroup>();
            if (cg == null) cg = ind.exclamationMark.AddComponent<CanvasGroup>();
            cg.alpha = available ? 1f : 0f;
        }

        // 注册完毕，统一通知一次
        if (StoryDotManager.Instance != null)
            StoryDotManager.Instance.NotifyStateChanged();

        Debug.Log("[ShowMapButton] 初始化感叹号状态完成");
    }

    /// <summary>
    /// 刷新地图上所有感叹号指示器
    /// </summary>
    private void RefreshAllIndicators()
    {
        var indicators = map.GetComponentsInChildren<StoryDotIndicator>(true);
        foreach (var ind in indicators)
        {
            ind.Refresh();
        }
    }

    public void ZoomTo(float scale)
    {
        scale = Mathf.Clamp(scale, minScale, maxScale);
        targetScale = new Vector3(scale, scale, 1f);
        mapRect.localScale = targetScale;
    }

    /// <summary>
    /// 立即把地图缩到最小
    /// </summary>
    public void ZoomToMin()
    {
        ZoomTo(0.9f);
    }

    /// <summary>
    /// 立即把地图缩到最大
    /// </summary>
    public void ZoomToMax()
    {
        ZoomTo(maxScale);
    }
}