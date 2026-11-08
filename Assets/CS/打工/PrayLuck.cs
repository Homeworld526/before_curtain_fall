using UnityEngine;
using System;
using System.Collections;

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

    [Header("点击反馈")]
    [SerializeField, Range(1f, 1.5f)] private float clickScale = 1.18f;
    [SerializeField, Min(0.01f)] private float clickAnimationDuration = 0.16f;

    [Header("骨气状态提示")]
    [SerializeField] private int lowBackboneUpperBound = 39;
    [SerializeField] private int uneasyBackboneUpperBound = 59;
    [SerializeField] private int steadyBackboneUpperBound = 79;
    [TextArea(2, 4)] [SerializeField] private string lowBackboneText = "烛火在你低垂的眼前摇晃，你一时不敢直视圣母像。";
    [TextArea(2, 4)] [SerializeField] private string uneasyBackboneText = "你在圣母像前停留许久，心里的迟疑仍没有散去。";
    [TextArea(2, 4)] [SerializeField] private string steadyBackboneText = "烛光落在你的肩上，你仍在思量该如何开口。";
    [TextArea(2, 4)] [SerializeField] private string firmBackboneText = "你抬眼望向圣母像，心中那句话已经有了答案。";

    Vector3 _originalScale;
    bool _isHovering;
    bool _hasPrayedThisWeek;
    bool _isShowingInteractionTip;
    bool _isPlayingClickFeedback;
    Vector3 _arrowOriginalPos;
    Coroutine _clickFeedbackCoroutine;

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
        // 清空静态变量，避免指向已销毁的对象
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
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

        if (!_isPlayingClickFeedback)
        {
            Vector3 targetScale = _isHovering ? _originalScale * hoverScale : _originalScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleLerpSpeed);
        }

        if (Input.GetMouseButtonDown(0) && _isHovering && !_isShowingInteractionTip)
        {
            if (_hasPrayedThisWeek)
                OnBackboneStatusClicked();
            else
                OnPrayClicked();
        }
    }

    void OnPrayClicked()
    {
        _hasPrayedThisWeek = true;
        _isShowingInteractionTip = true;
        PlayClickFeedback();

        if (arrowObject != null)
        {
            arrowObject.transform.localPosition = _arrowOriginalPos;
            arrowObject.SetActive(false);
        }

        if (JobManager.Instance == null)
            throw new InvalidOperationException("圣母像祈祷需要活动的 JobManager 实例。");

        JobManager.Instance.ShowPrayTip(OnPrayComplete);
    }

    void OnPrayComplete()
    {
        _isShowingInteractionTip = false;
    }

    void OnBackboneStatusClicked()
    {
        if (JobManager.Instance == null)
            throw new InvalidOperationException("圣母像骨气提示需要活动的 JobManager 实例。");

        _isShowingInteractionTip = true;
        PlayClickFeedback();
        JobManager.Instance.ShowBackboneTip(GetBackboneStatusText(), () => _isShowingInteractionTip = false);
    }

    private void PlayClickFeedback()
    {
        if (_clickFeedbackCoroutine != null)
            StopCoroutine(_clickFeedbackCoroutine);

        _clickFeedbackCoroutine = StartCoroutine(PlayClickFeedbackCoroutine());
    }

    private IEnumerator PlayClickFeedbackCoroutine()
    {
        _isPlayingClickFeedback = true;

        Vector3 pressedScale = _originalScale * clickScale;
        yield return ScaleTo(transform.localScale, pressedScale, clickAnimationDuration * 0.5f);

        Vector3 restingScale = _isHovering ? _originalScale * hoverScale : _originalScale;
        yield return ScaleTo(pressedScale, restingScale, clickAnimationDuration * 0.5f);

        _isPlayingClickFeedback = false;
        _clickFeedbackCoroutine = null;
    }

    private IEnumerator ScaleTo(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        transform.localScale = to;
    }

    private string GetBackboneStatusText()
    {
        int backbone = BackboneSystem.CurrentValue;
        if (backbone <= lowBackboneUpperBound)
            return lowBackboneText;
        if (backbone <= uneasyBackboneUpperBound)
            return uneasyBackboneText;
        if (backbone <= steadyBackboneUpperBound)
            return steadyBackboneText;

        return firmBackboneText;
    }

    public void SetPrayed()
    {
        _hasPrayedThisWeek = true;
        if (arrowObject != null) arrowObject.SetActive(false);
    }

    public void ResetWeeklyState()
    {
        _hasPrayedThisWeek = false;
        _isShowingInteractionTip = false;

        if (arrowObject != null)
        {
            arrowObject.transform.localPosition = _arrowOriginalPos;
            arrowObject.SetActive(true);
        }
    }
}
