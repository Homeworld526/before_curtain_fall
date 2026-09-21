using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CanvasGroupFader
{
    private static Dictionary<CanvasGroup, CoroutineInfo> _fadeCoroutines = new Dictionary<CanvasGroup, CoroutineInfo>();

    private class CoroutineInfo
    {
        public Coroutine routine;
        public System.Action callback;
    }

    public static void FadeTo(CanvasGroup canvasGroup, float targetAlpha, float duration)
    {
        FadeTo(canvasGroup, targetAlpha, duration, null);
    }

    public static void FadeTo(CanvasGroup canvasGroup, float targetAlpha, float duration, System.Action onComplete)
    {
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup is null");
            return;
        }

        if (duration <= 0)
        {
            canvasGroup.alpha = targetAlpha;
            onComplete?.Invoke();
            return;
        }

        MonoBehaviour coroutineRunner = GetCoroutineRunner();
        
        if (_fadeCoroutines.ContainsKey(canvasGroup) && _fadeCoroutines[canvasGroup].routine != null)
        {
            Debug.Log($"[CanvasGroupFader] 停止 CanvasGroup 的旧淡入淡出协程");
            coroutineRunner.StopCoroutine(_fadeCoroutines[canvasGroup].routine);
            _fadeCoroutines[canvasGroup].callback?.Invoke();
        }

        CoroutineInfo info = new CoroutineInfo();
        info.callback = onComplete;
        info.routine = coroutineRunner.StartCoroutine(FadeRoutine(canvasGroup, targetAlpha, duration, () => {
            info.callback?.Invoke();
            _fadeCoroutines.Remove(canvasGroup);
        }));
        _fadeCoroutines[canvasGroup] = info;
    }

    public static void FadeIn(CanvasGroup canvasGroup, float duration)
    {
        FadeTo(canvasGroup, 1f, duration);
    }

    public static void FadeIn(CanvasGroup canvasGroup, float duration, System.Action onComplete)
    {
        FadeTo(canvasGroup, 1f, duration, onComplete);
    }

    public static void FadeOut(CanvasGroup canvasGroup, float duration)
    {
        FadeTo(canvasGroup, 0f, duration);
    }

    public static void FadeOut(CanvasGroup canvasGroup, float duration, System.Action onComplete)
    {
        FadeTo(canvasGroup, 0f, duration, onComplete);
    }

    public static void CancelFade(CanvasGroup canvasGroup, float resetAlpha = -1f)
    {
        if (canvasGroup == null) return;
        
        if (_fadeCoroutines.ContainsKey(canvasGroup))
        {
            MonoBehaviour coroutineRunner = GetCoroutineRunner();
            if (_fadeCoroutines[canvasGroup].routine != null)
            {
                coroutineRunner.StopCoroutine(_fadeCoroutines[canvasGroup].routine);
            }
            _fadeCoroutines.Remove(canvasGroup);
        }
        
        if (resetAlpha >= 0f)
        {
            canvasGroup.alpha = resetAlpha;
        }
    }

    private static IEnumerator FadeRoutine(CanvasGroup canvasGroup, float targetAlpha, float duration, System.Action onComplete)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        yield return null;
        onComplete?.Invoke();
    }

    private static MonoBehaviour GetCoroutineRunner()
    {
        GameObject runnerObject = GameObject.Find("_CoroutineRunner");
        if (runnerObject == null)
        {
            runnerObject = new GameObject("_CoroutineRunner");
            Object.DontDestroyOnLoad(runnerObject);
        }

        MonoBehaviour runner = runnerObject.GetComponent<MonoBehaviour>();
        if (runner == null)
        {
            runner = runnerObject.AddComponent<CoroutineRunner>();
        }

        return runner;
    }
}

public class CoroutineRunner : MonoBehaviour
{
}
