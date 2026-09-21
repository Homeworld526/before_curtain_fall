using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHoverManager : MonoBehaviour
{
    public static UIHoverManager Instance;

    [Header("全局悬浮配置")]
    public float hoverScale = 1.2f;
    public float normalScale = 1f;
    public float smoothSpeed = 12f;

    [Header("手动拖拽需要悬浮的UI")]
    public List<RectTransform> hoverTargets;

    private class HoverItem
    {
        public RectTransform root;
        public RectTransform butim;      // 主图
        public RectTransform eff;        // 特效
        public bool isHovering;
    }

    private List<HoverItem> _hoverItems = new List<HoverItem>();

    void Awake()
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

    void Start()
    {
        RegisterAllManualTargets();
    }

    /// <summary>
    /// 注册手动拖进去的所有UI
    /// </summary>
    void RegisterAllManualTargets()
    {
        foreach (var rt in hoverTargets)
        {
            if (rt == null) continue;
            RegisterSingleUI(rt);
        }
    }

    /// <summary>
    /// 注册单个UI（自动找 butim & Eff）
    /// </summary>
    public void RegisterSingleUI(RectTransform btnRoot)
    {
        var butim = btnRoot.Find("butim")?.GetComponent<RectTransform>();
        var eff = btnRoot.Find("Eff")?.GetComponent<RectTransform>();

        if (butim == null)
        {
            Debug.LogWarning($"{btnRoot.name} 缺少 butim，跳过悬浮效果");
            return;
        }

        var item = new HoverItem
        {
            root = btnRoot,
            butim = butim,
            eff = eff,
            isHovering = false
        };

        if (eff != null)
            eff.gameObject.SetActive(false);

        _hoverItems.Add(item);
        BindHoverEvents(btnRoot, item);
    }

    /// <summary>
    /// 动态绑定 Pointer 事件，不用UI挂脚本
    /// </summary>
    void BindHoverEvents(RectTransform btnRoot, HoverItem item)
    {
        var trigger = btnRoot.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = btnRoot.gameObject.AddComponent<EventTrigger>();

        // 进入
        var enter = new EventTrigger.Entry();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener(_ =>
        {
            item.isHovering = true;
            if (item.eff != null)
                item.eff.gameObject.SetActive(true);
        });
        trigger.triggers.Add(enter);

        // 离开
        var exit = new EventTrigger.Entry();
        exit.eventID = EventTriggerType.PointerExit;
        exit.callback.AddListener(_ =>
        {
            item.isHovering = false;
            if (item.eff != null)
                item.eff.gameObject.SetActive(false);
        });
        trigger.triggers.Add(exit);
    }

    void Update()
    {
        foreach (var item in _hoverItems)
        {
            if (item == null) continue;

            float targetScale = item.isHovering ? hoverScale : normalScale;

            // 同步缩放 butim
            if (item.butim != null)
            {
                item.butim.localScale = Vector3.Lerp(
                    item.butim.localScale,
                    Vector3.one * targetScale,
                    smoothSpeed * Time.deltaTime
                );
            }

            // 同步缩放 Eff
            if (item.eff != null)
            {
                item.eff.localScale = Vector3.Lerp(
                    item.eff.localScale,
                    Vector3.one * targetScale,
                    smoothSpeed * Time.deltaTime
                );
            }
        }
    }
}