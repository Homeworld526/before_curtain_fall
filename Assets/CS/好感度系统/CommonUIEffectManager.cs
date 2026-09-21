using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CommonUIEffectManager : MonoBehaviour
{
    public static CommonUIEffectManager Instance { get; private set; }

    [Header("全局配置")]
    [Range(1.0f, 1.5f)]
    public float hoverScale = 1.1f;
    public float scaleDuration = 0.15f;
    public Ease hoverEase = Ease.OutQuad;
    public Ease exitEase = Ease.InQuad;

    [Header("自动扫描")]
    public bool autoScanOnStart = true;
    public List<Transform> scanRoots;

    private class EffectItem
    {
        public RectTransform root;
        public RectTransform eff;
        public bool isHovering;
        public Tweener currentTween;
        public Vector3 originalScale;
    }

    private List<EffectItem> effectItems = new List<EffectItem>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (autoScanOnStart)
        {
            AutoScanAllUIs();
        }

        if (scanRoots != null)
        {
            foreach (var root in scanRoots)
            {
                if (root != null)
                {
                    ScanUIsInChildren(root);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AutoScanAllUIs()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            ScanUIsInChildren(canvas.transform);
        }
    }

    public void ScanUIsInChildren(Transform parent)
    {
        if (parent == null) return;

        foreach (Transform child in parent)
        {
            if (child.name == "Eff")
            {
                RectTransform parentRect = child.parent as RectTransform;
                if (parentRect != null)
                {
                    RegisterUI(parentRect);
                }
            }
            else
            {
                ScanUIsInChildren(child);
            }
        }
    }

    public void RegisterUI(RectTransform uiRoot)
    {
        if (uiRoot == null) return;

        foreach (var existing in effectItems)
        {
            if (existing.root == uiRoot)
                return;
        }

        Transform effTransform = uiRoot.Find("Eff");
        RectTransform effRect = effTransform as RectTransform;

        var newItem = new EffectItem
        {
            root = uiRoot,
            eff = effRect,
            isHovering = false,
            originalScale = uiRoot.localScale
        };

        if (newItem.eff != null)
        {
            newItem.eff.gameObject.SetActive(false);
        }

        effectItems.Add(newItem);
        BindHoverEvents(uiRoot, newItem);
    }

    public void UnregisterUI(RectTransform uiRoot)
    {
        effectItems.RemoveAll(item =>
        {
            if (item.root == uiRoot)
            {
                if (item.currentTween != null && item.currentTween.active)
                {
                    item.currentTween.Kill();
                }
                return true;
            }
            return false;
        });
    }

    void BindHoverEvents(RectTransform uiRoot, EffectItem item)
    {
        var trigger = uiRoot.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = uiRoot.gameObject.AddComponent<EventTrigger>();

        bool hasEnter = false;
        bool hasExit = false;
        foreach (var entry in trigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerEnter)
                hasEnter = true;
            if (entry.eventID == EventTriggerType.PointerExit)
                hasExit = true;
        }

        if (!hasEnter)
        {
            var enter = new EventTrigger.Entry();
            enter.eventID = EventTriggerType.PointerEnter;
            enter.callback.AddListener(_ =>
            {
                OnHoverEnter(item);
            });
            trigger.triggers.Add(enter);
        }

        if (!hasExit)
        {
            var exit = new EventTrigger.Entry();
            exit.eventID = EventTriggerType.PointerExit;
            exit.callback.AddListener(_ =>
            {
                OnHoverExit(item);
            });
            trigger.triggers.Add(exit);
        }
    }

    void OnHoverEnter(EffectItem item)
    {
        if (item == null || item.root == null) return;

        item.isHovering = true;

        if (item.currentTween != null && item.currentTween.active)
        {
            item.currentTween.Kill();
        }

        item.currentTween = item.root.DOScale(item.originalScale * hoverScale, scaleDuration)
            .SetEase(hoverEase);

        if (item.eff != null)
        {
            item.eff.gameObject.SetActive(true);
        }
    }

    void OnHoverExit(EffectItem item)
    {
        if (item == null || item.root == null) return;

        item.isHovering = false;

        if (item.currentTween != null && item.currentTween.active)
        {
            item.currentTween.Kill();
        }

        item.currentTween = item.root.DOScale(item.originalScale, scaleDuration)
            .SetEase(exitEase);

        if (item.eff != null)
        {
            item.eff.gameObject.SetActive(false);
        }
    }

    public void SetHoverScale(float scale)
    {
        hoverScale = Mathf.Clamp(scale, 1.0f, 1.5f);
    }

    public void SetEffectActive(Transform uiRoot, bool active)
    {
        var item = effectItems.Find(i => i.root == uiRoot);
        if (item != null && item.eff != null)
        {
            item.eff.gameObject.SetActive(active);
        }
    }
}
