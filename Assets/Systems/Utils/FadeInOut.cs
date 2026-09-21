using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CanvasGroup 渐入渐出工具类
/// 提供静态方法实现任意CanvasGroup的淡入淡出效果
/// </summary>
public static class FadeInOut
{
    /// <summary>
    /// 存储每个CanvasGroup对应的协程信息
    /// </summary>
    private class FadeInfo
    {
        public Coroutine Coroutine;
        public MonoBehaviour MonoBehaviour;
        public bool IsStopped;
    }

    private static readonly Dictionary<CanvasGroup, FadeInfo> _activeCoroutines = new Dictionary<CanvasGroup, FadeInfo>();

    /// <summary>
    /// 渐入效果（淡入到指定透明度）
    /// </summary>
    /// <param name="canvasGroup">目标CanvasGroup</param>
    /// <param name="targetAlpha">目标透明度（0-1）</param>
    /// <param name="duration">持续时间</param>
    /// <param name="monoBehaviour">用于启动协程的MonoBehaviour实例</param>
    /// <returns>协程对象，可用于手动停止</returns>
    public static Coroutine FadeIn(CanvasGroup canvasGroup, float targetAlpha = 1f, float duration = 0.5f, MonoBehaviour monoBehaviour = null)
    {
        return Fade(canvasGroup, targetAlpha, duration, monoBehaviour);
    }

    /// <summary>
    /// 渐出效果（淡出到指定透明度）
    /// </summary>
    /// <param name="canvasGroup">目标CanvasGroup</param>
    /// <param name="targetAlpha">目标透明度（0-1）</param>
    /// <param name="duration">持续时间</param>
    /// <param name="monoBehaviour">用于启动协程的MonoBehaviour实例</param>
    /// <returns>协程对象，可用于手动停止</returns>
    public static Coroutine FadeOut(CanvasGroup canvasGroup, float targetAlpha = 0f, float duration = 0.5f, MonoBehaviour monoBehaviour = null)
    {
        return Fade(canvasGroup, targetAlpha, duration, monoBehaviour);
    }

    /// <summary>
    /// 通用渐变效果（从当前透明度渐变到目标透明度）
    /// </summary>
    /// <param name="canvasGroup">目标CanvasGroup</param>
    /// <param name="targetAlpha">目标透明度（0-1）</param>
    /// <param name="duration">持续时间</param>
    /// <param name="monoBehaviour">用于启动协程的MonoBehaviour实例</param>
    /// <returns>协程对象，可用于手动停止</returns>
    public static Coroutine Fade(CanvasGroup canvasGroup, float targetAlpha, float duration, MonoBehaviour monoBehaviour = null)
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("FadeInOut: CanvasGroup 为 null");
            return null;
        }

        if (monoBehaviour == null)
        {
            Debug.LogWarning("FadeInOut: MonoBehaviour 为 null，无法启动协程");
            return null;
        }

        // 如果该CanvasGroup已有正在运行的协程，先停止
        StopFade(canvasGroup);

        var fadeInfo = new FadeInfo { MonoBehaviour = monoBehaviour };
        Coroutine coroutine = monoBehaviour.StartCoroutine(FadeCoroutine(canvasGroup, targetAlpha, duration, fadeInfo));
        fadeInfo.Coroutine = coroutine;
        _activeCoroutines[canvasGroup] = fadeInfo;
        return coroutine;
    }

    /// <summary>
    /// 停止指定CanvasGroup的渐变协程
    /// </summary>
    /// <param name="canvasGroup">目标CanvasGroup</param>
    public static void StopFade(CanvasGroup canvasGroup)
    {
        if (canvasGroup != null && _activeCoroutines.TryGetValue(canvasGroup, out FadeInfo fadeInfo))
        {
            fadeInfo.IsStopped = true;
            fadeInfo.MonoBehaviour.StopCoroutine(fadeInfo.Coroutine);
            _activeCoroutines.Remove(canvasGroup);
        }
    }

    /// <summary>
    /// 停止所有正在运行的渐变协程
    /// </summary>
    public static void StopAllFades()
    {
        foreach (var fadeInfo in _activeCoroutines.Values)
        {
            if (fadeInfo?.MonoBehaviour != null && fadeInfo.Coroutine != null)
            {
                fadeInfo.IsStopped = true;
                fadeInfo.MonoBehaviour.StopCoroutine(fadeInfo.Coroutine);
            }
        }
        _activeCoroutines.Clear();
    }

    /// <summary>
    /// 实际执行的渐变协程
    /// </summary>
    private static IEnumerator FadeCoroutine(CanvasGroup canvasGroup, float targetAlpha, float duration, FadeInfo fadeInfo)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration && !fadeInfo.IsStopped)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 使用平滑函数
            t = Mathf.SmoothStep(0f, 1f, t);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        // 如果不是被停止的，确保最终值准确
        if (!fadeInfo.IsStopped)
        {
            canvasGroup.alpha = targetAlpha;
        }
        
        // 完成后移除字典中的引用
        _activeCoroutines.Remove(canvasGroup);
    }
}
