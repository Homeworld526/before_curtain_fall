using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    // true=可弹出，弹出一次后变false，只有新游戏时重置为true
    private static bool _canAutoShow = true;

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EditorReset()
    {
        _canAutoShow = true;
    }
#endif

    [Header("教学UI")]
    public GameObject tutorialUI;

    public List<GameObject> Button = new List<GameObject>();

    // 防止重复添加关闭按钮监听
    private bool _closeButtonHooked = false;
    private Button _cachedCloseBtn;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        CloseAll();
        HookCloseButton();
    }

    /// <summary>
    /// 弹出教学（自动触发，仅新游戏首次进场景弹一次）
    /// </summary>
    public void ShowTutorialUI()
    {
        CloseAll();

        if (!_canAutoShow)
        {
            Debug.Log("[教学] 本次已弹过，跳过");
            return;
        }

        if (tutorialUI == null)
        {
            Debug.LogWarning("[教学] tutorialUI 未赋值");
            return;
        }

        // 设置 openedByAuto 标记
        var revealer = tutorialUI.GetComponentInChildren<TutorialStepRevealer>(true);
        if (revealer != null)
        {
            revealer.openedByAuto = true;
        }

        // 给关闭按钮添加点击监听（仅点击关闭按钮才触发引导）
        HookCloseButton();

        foreach (var btn in Button)
        {
            if (btn != null && !btn.activeInHierarchy)
                btn.SetActive(true);
        }

        tutorialUI.SetActive(true);
        _canAutoShow = false;
        Debug.Log("[教学] 教学已弹出");
    }

    /// <summary>
    /// 手动打开教学（按钮调用，不受限制）
    /// </summary>
    public void ShowManualTutorial()
    {
        CloseAll();
        if (tutorialUI == null) return;
        
        
        foreach (var btn in Button)
        {
            if (btn != null && !btn.activeInHierarchy)
                btn.SetActive(true);
        }

        TutorialStepRevealer.nextOpenByButton = true;
        tutorialUI.SetActive(true);
        Debug.Log("[教学] 手动打开教学");
    }

    public void CloseAll()
    {
        if (tutorialUI != null) tutorialUI.SetActive(false);
    }

    /// <summary>
    /// 给教学UI的"关闭"按钮添加点击监听
    /// </summary>
    private void HookCloseButton()
    {
        if (tutorialUI == null) return;

        Transform closeBtnTransform = FindChildRecursive(tutorialUI.transform, "关闭");
        if (closeBtnTransform == null)
        {
            Debug.LogWarning("[教学] 找不到名为'关闭'的按钮");
            return;
        }

        Button closeBtn = closeBtnTransform.GetComponent<Button>();
        if (closeBtn == null)
        {
            Debug.LogWarning("[教学] '关闭'物体上没有 Button 组件");
            return;
        }

        if (_cachedCloseBtn != closeBtn)
        {
            if (_cachedCloseBtn != null)
                _cachedCloseBtn.onClick.RemoveListener(OnCloseButtonClicked);
            _cachedCloseBtn = closeBtn;
            _closeButtonHooked = false;
        }

        if (!_closeButtonHooked)
        {
            closeBtn.onClick.RemoveListener(OnCloseButtonClicked);
            closeBtn.onClick.AddListener(OnCloseButtonClicked);
            _closeButtonHooked = true;
            Debug.Log("[教学] 已给关闭按钮添加引导触发监听");
        }
    }

    /// <summary>
    /// 关闭按钮被点击时调用。延迟一帧执行，确保教学UI先关闭。
    /// </summary>
    private void OnCloseButtonClicked()
    {
        Debug.Log("[教学] 关闭按钮被点击，延迟一帧触发引导");
        StartCoroutine(DelayedStartGuide());
    }

    private System.Collections.IEnumerator DelayedStartGuide()
    {
        // 等一帧，让 SetActive(false) 先执行完毕
        yield return null;
        OnTutorialBookletClosed();
    }

    /// <summary>
    /// 教学册子关闭后调用，启动强制引导
    /// </summary>
    public void OnTutorialBookletClosed()
    {
        if (TutorialGuideManager.Instance != null && !TutorialGuideManager.IsGuideCompleted())
        {
            Debug.Log("[教学] 教学册子已关闭，启动强制引导");
            TutorialGuideManager.Instance.StartGuide();
        }
    }

    /// <summary>
    /// 递归查找子物体
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildRecursive(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    public void ResetForNewGame()
    {
        _canAutoShow = true;
        _closeButtonHooked = false;
        _cachedCloseBtn = null;
        TutorialGuideManager.ResetForNewGame();
    }

    /// <summary>教学册子是否已弹出过（供存档系统使用）</summary>
    public static bool HasTutorialBeenShown() => !_canAutoShow;

    /// <summary>设置教学册子弹出状态（供存档系统使用）</summary>
    public static void SetTutorialShown(bool shown) => _canAutoShow = !shown;

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在存读档时调用）
    /// </summary>
    public static void ResetStaticState()
    {
        _canAutoShow = true;
        Debug.Log("[TutorialManager] 静态状态已重置: _canAutoShow=true");
    }
}
