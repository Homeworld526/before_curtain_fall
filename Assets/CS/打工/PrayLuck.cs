using UnityEngine;
using System;

public class PrayLuck : MonoBehaviour
{
    public static PrayLuck Instance { get; private set; }
    
    [Header("主控制器")]
    public bool isMainController = true;

    [Header("箭头对象")]
    public GameObject arrowObject;
    public float floatAmplitude = 0.15f;
    public float floatSpeed = 2f;

    [Header("悬浮缩放")]
    public float hoverScale = 1.1f;
    public float scaleLerpSpeed = 10f;

    SpriteRenderer _spriteRenderer;
    Vector3 _originalScale;
    bool _isHovering;
    bool _hasPrayedThisWeek;
    Vector3 _arrowOriginalPos;

    public bool HasPrayedThisWeek => _hasPrayedThisWeek;

    void Awake()
    {
        Debug.Log($"[PrayLuck] Awake被调用! 对象名: {gameObject.name}, isMainController: {isMainController}, Instance状态: {(Instance == null ? "null" : "已存在")}");
        
        if (isMainController)
        {
            if (Instance == null)
            {
                Instance = this;
                Debug.Log($"[PrayLuck] 设置为单例Instance (主控制器)");
            }
            else if (Instance != this)
            {
                Debug.LogWarning($"[PrayLuck] 检测到多个主控制器实例，准备销毁当前对象! 现有Instance: {Instance.gameObject.name}");
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.Log($"[PrayLuck] 非主控制器对象，跳过单例检查");
        }
    }

    void OnDestroy()
    {
        Debug.LogWarning($"[PrayLuck] 对象被销毁! 对象名: {gameObject.name}, 调用栈:\n{System.Environment.StackTrace}");
        
        // 清空静态变量，避免指向已销毁的对象
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalScale = transform.localScale;

        if (arrowObject != null)
            _arrowOriginalPos = arrowObject.transform.localPosition;
    }

    void Update()
    {
        if (!isMainController) return;

        UpdateArrowFloat();
        UpdateHoverAndClick();
    }

    void UpdateArrowFloat()
    {
        if (arrowObject == null) return;

        if (!_hasPrayedThisWeek)
        {
            float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            arrowObject.transform.localPosition = _arrowOriginalPos + new Vector3(0, yOffset, 0);
            if (!arrowObject.activeSelf)
                arrowObject.SetActive(true);
        }
    }

    void UpdateHoverAndClick()
    {
        // 地图打开时屏蔽场景点击穿透
        if (ShowMapButton.IsMapConsumingInput) return;
        
        // 检查 DialogList.Instance 是否存在且激活
        if (DialogList.Instance != null && DialogList.Instance.gameObject.activeInHierarchy) return;

        // 引导期间屏蔽点击
        if (TutorialGuideManager.IsGuideActive)
        {
            Debug.Log("[PrayLuck] 引导期间屏蔽点击");
            return;
        }

        // 检测鼠标是否在UI上（检查所有UI遮挡）
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        _isHovering = hit.collider != null && (hit.collider.gameObject == gameObject || hit.collider.transform.parent == transform);

        if (_isHovering && !_hasPrayedThisWeek)
            transform.localScale = Vector3.Lerp(transform.localScale, _originalScale * hoverScale, Time.deltaTime * scaleLerpSpeed);
        else
            transform.localScale = Vector3.Lerp(transform.localScale, _originalScale, Time.deltaTime * scaleLerpSpeed);

        if (Input.GetMouseButtonDown(0) && _isHovering && !_hasPrayedThisWeek)
            OnPrayClicked();
    }

    void OnPrayClicked()
    {
        _hasPrayedThisWeek = true;

        if (arrowObject != null)
        {
            arrowObject.transform.localPosition = _arrowOriginalPos;
            arrowObject.SetActive(false);
        }

        if (_spriteRenderer != null)
            _spriteRenderer.enabled = false;

        if (JobManager.Instance != null)
            JobManager.Instance.ShowPrayTip(OnPrayComplete);
    }

    void OnPrayComplete()
    {
    }

    public void SetPrayed()
    {
        _hasPrayedThisWeek = true;
        if (arrowObject != null) arrowObject.SetActive(false);
        if (_spriteRenderer != null) _spriteRenderer.enabled = false;
    }

    public void ResetWeeklyState()
    {
        _hasPrayedThisWeek = false;

        if (arrowObject != null)
        {
            arrowObject.transform.localPosition = _arrowOriginalPos;
            arrowObject.SetActive(true);
        }

        if (_spriteRenderer != null)
            _spriteRenderer.enabled = true;
    }
}
