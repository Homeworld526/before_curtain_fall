using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// 全局 UI 淡入淡出管理器
/// 用法：UIFadeManager.FadeIn(targetGameObject);
/// </summary>
public class UIFadeManager : MonoBehaviour
{
    // 单例实例
    private static UIFadeManager _instance;
    public static UIFadeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("[UIFadeManager]");
                _instance = go.AddComponent<UIFadeManager>();
                DontDestroyOnLoad(go); // 跨场景保留（可选）
            }
            return _instance;
        }
    }

    // 默认配置
    [Header("Default Settings")]
    public float defaultDuration = 0.3f;
    public Ease defaultEase = Ease.InOutQuad;

    // 缓存 CanvasGroup，避免重复获取
    private Dictionary<GameObject, CanvasGroup> _groupCache = new Dictionary<GameObject, CanvasGroup>();

    /// <summary>
    /// 【通用接口】淡入 (0 -> 1)
    /// </summary>
    public static void FadeIn(GameObject target, float? duration = null, System.Action onComplete = null)
    {
        if (target == null) return;

        float time = duration ?? Instance.defaultDuration;
        CanvasGroup group = GetOrCreateCanvasGroup(target);

        // 确保初始状态正确
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        target.SetActive(true); // 确保物体是激活的

        // 【修复点】使用 DOTween.Kill 停止该对象上所有的 Tween
        DOTween.Kill(group);

        group.DOFade(1f, time)
            .SetEase(Instance.defaultEase)
            .OnComplete(() =>
            {
                group.interactable = true;
                group.blocksRaycasts = true;
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// 【通用接口】淡出 (1 -> 0)
    /// </summary>
    public static void FadeOut(GameObject target, float? duration = null, bool hideAfter = true, System.Action onComplete = null)
    {
        if (target == null) return;

        float time = duration ?? Instance.defaultDuration;
        CanvasGroup group = GetOrCreateCanvasGroup(target);

        // 立即禁止交互，防止用户在淡出过程中点击
        group.interactable = false;
        group.blocksRaycasts = false;

        // 【修复点】使用 DOTween.Kill 停止该对象上所有的 Tween
        DOTween.Kill(group);

        group.DOFade(0f, time)
            .SetEase(Instance.defaultEase)
            .OnComplete(() =>
            {
                if (hideAfter)
                {
                    target.SetActive(false);
                }
                // 重置 alpha 以防下次复用出错
                group.alpha = 0f;
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// 【通用接口】切换显示状态
    /// </summary>
    public static void Toggle(GameObject target, float? duration = null)
    {
        if (!target.activeSelf)
        {
            FadeIn(target, duration);
        }
        else
        {
            FadeOut(target, duration);
        }
    }

    // 内部辅助：获取或创建 CanvasGroup
    public static CanvasGroup GetOrCreateCanvasGroup(GameObject target)
    {
        if (Instance._groupCache.TryGetValue(target, out var group))
        {
            if (group != null) return group;
        }

        group = target.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = target.AddComponent<CanvasGroup>();
        }

        Instance._groupCache[target] = group;
        return group;
    }

    // 清理缓存
    private void OnDestroy()
    {
        _groupCache.Clear();

        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// 重置静态状态（由 StaticStateResetManager 在场景切换时调用）
    /// 注意：不使用 Destroy，因为 Destroy 是延迟执行的，
    /// 会导致新场景的 Instance 误判为"已存在"而销毁新物体
    /// </summary>
    public static void ResetStaticState()
    {
        // 只清空 Instance，不销毁物体
        _instance = null;
        Debug.Log("[UIFadeManager] 静态状态已重置，Instance 已清空");
    }
}